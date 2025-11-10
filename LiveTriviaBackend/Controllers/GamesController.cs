using live_trivia.Repositories;
using live_trivia.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace live_trivia.Controllers
{
    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        private readonly GamesRepository _repository;

        public GamesController(GamesRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{roomId}")]
        [Authorize]
        public async Task<IActionResult> GetGame(string roomId)
        {
            var details = await _repository.GetGameDetailsAsync(roomId);
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
            var player = await _repository.GetPlayerByIdAsync(playerId);
            if (player == null) return Unauthorized("Associated player profile not found.");

            var game = await _repository.CreateGameAsync(roomId, player);
            await _repository.AddExistingPlayerToGameAsync(game, player);


            return Created($"/games/{roomId}", game);
        }

        [HttpPost("{roomId}/start")]
        [Authorize]
        public async Task<IActionResult> StartGame(string roomId)
        {
            var success = await _repository.StartGameAsync(roomId);

            if (!success)
                return BadRequest("Game could not be started. Make sure there are players and questions.");

            return Ok(new { message = "Game started successfully." });
        }

        [HttpPost("{roomId}/next")]
        [Authorize]
        public async Task<IActionResult> NextQuestion(string roomId)
        {
            var game = await _repository.GetGameAsync(roomId);
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
            await _repository.SaveChangesAsync();

            if (!moved)
                return Ok(new { message = "Game finished.", state = game.State.ToString() });

            return Ok(new { message = "Moved to next question.", questionIndex = game.CurrentQuestionIndex });
        }
        [HttpPost("{roomId}/join")]
        [Authorize]
        public async Task<IActionResult> JoinGame(string roomId)
        {
            // 1. SECURELY GET PLAYER ID from JWT Claim
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }

            var game = await _repository.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            // 2. CHECK IF PLAYER IS ALREADY IN GAME
            if (game.GamePlayers.Any(gp => gp.PlayerId == playerId))
            {
                return BadRequest("Player already in this game.");
            }

            var existingPlayer = await _repository.GetPlayerByIdAsync(playerId);

            if (existingPlayer == null)
            {
                return Unauthorized("Associated player profile not found.");
            }

            await _repository.AddExistingPlayerToGameAsync(game, existingPlayer);

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

            var game = await _repository.GetGameAsync(roomId);
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

            await _repository.SaveChangesAsync();

            return Ok(playerAnswer);
        }
        [HttpGet("{roomId}/settings")]
        [Authorize]
        public async Task<IActionResult> GetSettings(string roomId)
        {
            var settings = await _repository.GetGameSettingsAsync(roomId);
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
            var game = await _repository.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            // 1. SECURELY GET PLAYER ID from JWT Claim
            var playerIdClaim = User.FindFirst("playerId");
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out int playerId))
            {
                return Unauthorized("Authenticated player identity not found.");
            }
            
            // Only host can update settings
            if (game.HostPlayerId != playerId)
                return Forbid("Only the host can modify game settings.");

            var updated = await _repository.UpdateGameSettingsAsync(roomId, dto);
            return Ok(updated);
        }

    }
}
