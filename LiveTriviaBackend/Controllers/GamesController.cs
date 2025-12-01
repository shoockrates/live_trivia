using live_trivia.Dtos;
using live_trivia.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using live_trivia.Interfaces;
using live_trivia.Exceptions;
using Serilog;

namespace live_trivia.Controllers
{
    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IGameService _gameService;
        private readonly IActiveGamesService _activeGamesService;

        public GamesController(IGameService gameService, IHubContext<GameHub> hubContext, IActiveGamesService activeGamesService)
        {
            _gameService = gameService;
            _hubContext = hubContext;
            _activeGamesService = activeGamesService;
        }

        [HttpGet("{roomId}")]
        [Authorize]
        public async Task<IActionResult> GetGame(string roomId)
        {
            var details = await _gameService.GetGameDetailsAsync(roomId);
            if (details == null)
                return NotFound("Game not found");

            return Ok(details);
        }

        [HttpPost("{roomId}")]
        [Authorize]
        public async Task<IActionResult> CreateGame(string roomId)
        {
            // 1. SECURELY GET PLAYER ID from JWT Claim
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }

            // 2. Get the Player entity
            var player = await _gameService.GetPlayerByIdAsync(playerId);
            if (player == null) return Unauthorized("Associated player profile not found.");

            var game = await _gameService.CreateGameAsync(roomId, player);
            await _gameService.AddExistingPlayerToGameAsync(game, player);


            return Created($"/games/{roomId}", game);
        }

        [HttpPost("{roomId}/start")]
        [Authorize]
        public async Task<IActionResult> StartGame(string roomId)
        {
            try
            {
                var success = await _gameService.StartGameAsync(roomId);

                if (!success)
                    return BadRequest("Game could not be started. Make sure there are players and questions.");

                // Notify all clients
                var gameDetails = await _gameService.GetGameDetailsAsync(roomId);
                await _hubContext.Clients.Group(roomId).SendAsync("GameStarted", gameDetails);

                return Ok(new { message = "Game started successfully." });
            }
            catch (NotEnoughQuestionsException ex)
            {
                Log.Error(ex,
                    "Failed to start game for room {RoomId}: Not enough questions in category {Category}. Needed {Count}.",
                    roomId, ex.Category, ex.RequiredCount);
                return BadRequest(new
                {
                    message = $"Not enough questions available.",
                    category = ex.Category,
                    required = ex.RequiredCount
                });
            }
        }

        [HttpPost("{roomId}/next")]
        [Authorize]
        public async Task<IActionResult> NextQuestion(string roomId)
        {
            var game = await _gameService.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            // Only host can advance
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                return Unauthorized("Authenticated player identity not found.");

            if (game.HostPlayerId != playerId)
                return Forbid("Only the host can advance the game.");

            // Before moving, score the current question
            game.ScoreCurrentQuestion();

            var moved = game.MoveNextQuestion();
            await _gameService.SaveChangesAsync();

            if (!moved)
            {
                // Game finished - broadcast to all clients
                var leaderboard = game.GetLeaderboard();
                await _hubContext.Clients.Group(roomId).SendAsync("GameFinished", new
                {
                    Leaderboard = leaderboard,
                    FinalScores = leaderboard.Select(p => new { p.Name, p.Score })
                });

                await Task.Delay(5000);
                await _gameService.CleanupGameAsync(roomId);

                return Ok(new { message = "Game finished.", state = game.State.ToString() });
            }

            // Broadcast the next question to all clients
            var gameDetails = await _gameService.GetGameDetailsAsync(roomId);
            await _hubContext.Clients.Group(roomId).SendAsync("NextQuestion", gameDetails);

            return Ok(new { message = "Moved to next question.", questionIndex = game.CurrentQuestionIndex });
        }


        [HttpPost("{roomId}/join")]
        [Authorize]
        public async Task<IActionResult> JoinGame(string roomId)
        {
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }

            var game = await _gameService.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            if (game.GamePlayers.Any(gp => gp.PlayerId == playerId))
            {
                return BadRequest("Player already in this game.");
            }

            var existingPlayer = await _gameService.GetPlayerByIdAsync(playerId);

            if (existingPlayer == null)
            {
                return Unauthorized("Associated player profile not found.");
            }

            await _gameService.AddExistingPlayerToGameAsync(game, existingPlayer);

            // Notify all clients in the room
            var gameDetails = await _gameService.GetGameDetailsAsync(roomId);
            await _hubContext.Clients.Group(roomId).SendAsync("PlayerJoined", new
            {
                Player = new { existingPlayer.Id, existingPlayer.Name },
                GameState = gameDetails
            });

            return Ok(existingPlayer);
        }
        [HttpPost("{roomId}/answer")]
        [Authorize]
        public async Task<IActionResult> SubmitAnswer(string roomId, [FromBody] AnswerRequest request)
        {
            // The claim is stored as a string, so we must parse it to an int.
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Player identity not found in token.");
            }

            var game = await _gameService.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var player = game.GamePlayers.FirstOrDefault(gp => gp.PlayerId == playerId)?.Player;
            if (player == null)
                return NotFound("Player not found in this game");

            var question = game.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
            if (question == null)
                return NotFound("Question not found in this game");

            var playerAnswer = new PlayerAnswer
            {
                PlayerId = player.Id,
                QuestionId = question.Id,
                GameRoomId = roomId,
                SelectedAnswerIndexes = request.SelectedAnswerIndexes,
                AnsweredAt = DateTime.UtcNow
            };

            game.PlayerAnswers.Add(playerAnswer);

            await _gameService.SaveChangesAsync();

            return Ok(playerAnswer);
        }

        [HttpGet("{roomId}/settings")]
        [Authorize]
        public async Task<IActionResult> GetSettings(string roomId)
        {
            var settings = await _gameService.GetGameSettingsAsync(roomId);
            if (settings == null)
                return NotFound("Settings not found");

            return Ok(new GameSettingsDto
            {
                Category = settings.Category,
                Difficulty = settings.Difficulty,
                QuestionCount = settings.QuestionCount,
                TimeLimitSeconds = settings.TimeLimitSeconds,
            });
        }

        [HttpPost("{roomId}/settings")]
        [Authorize]
        public async Task<IActionResult> UpdateSettings(string roomId, [FromBody] GameSettingsDto dto)
        {
            var game = await _gameService.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }

            // Only host can update settings
            if (game.HostPlayerId != playerId)
                return Forbid("Only the host can modify game settings.");

            var updated = await _gameService.UpdateGameSettingsAsync(roomId, dto);
            return Ok(updated);
        }

        [HttpGet("active")]
        public IActionResult GetActiveGames()
        {
            var activeGames = _activeGamesService.GetActiveGameIds();
            return Ok(activeGames);
        }

        [HttpDelete("{roomId}")]
        [Authorize]
        public async Task<IActionResult> DeleteGame(string roomId)
        {
            var game = await _gameService.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
                return Unauthorized("Authenticated player identity not found.");

            // Only host can delete
            if (game.HostPlayerId != playerId)
                return Forbid("Only the host can delete the game.");

            await _gameService.CleanupGameAsync(roomId);
            return Ok(new { message = "Game deleted successfully." });
        }

        [HttpPost("{roomId}/vote")]
        [Authorize]
        public async Task<IActionResult> SubmitVote(string roomId, [FromBody] VoteRequest request)
        { 
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }

            try
            {
                await _gameService.RecordCategoryVoteAsync(roomId, playerId, request.Category); 
                return Ok(new { message = $"Vote for {request.Category} recorded." });
            }
            catch (TriviaException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message }); 
            }
        }
    }
}
