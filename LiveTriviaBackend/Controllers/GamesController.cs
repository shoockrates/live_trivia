using live_trivia.Repositories;
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
        public async Task<IActionResult> CreateGame(string roomId)
        {
            var game = await _repository.CreateGameAsync(roomId);
            return Created($"/games/{roomId}", game);
        }

        [HttpPost("{roomId}/join")]
        public async Task<IActionResult> JoinGame(string roomId, [FromQuery] string playerName)
        {
            var game = await _repository.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var player = await _repository.AddPlayerAsync(game, playerName);
            return Ok(player);
        }

        [HttpPost("{roomId}/answer")]
        public async Task<IActionResult> SubmitAnswer(string roomId, [FromBody] AnswerRequest request)
        {
            var game = await _repository.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var player = game.GamePlayers.FirstOrDefault(gp => gp.PlayerId == request.PlayerId)?.Player;
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
