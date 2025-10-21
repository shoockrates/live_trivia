using live_trivia.Repositories;
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

        [HttpPost("{roomId}")]
        [Authorize]
        public async Task<IActionResult> CreateGame(string roomId)
        {
            var game = await _repository.CreateGameAsync(roomId);
            return Created($"/games/{roomId}", game);
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

    }
}
