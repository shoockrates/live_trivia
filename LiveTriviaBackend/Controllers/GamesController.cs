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

        [HttpPost("{roomId}/players")]
        public async Task<IActionResult> JoinGame(string roomId, [FromQuery] string playerName)
        {
            var game = await _repository.GetGameAsync(roomId);
            if (game == null)
                return NotFound("Game not found");

            var player = await _repository.AddPlayerAsync(game, playerName);
            return Ok(player);
        }
    }
}
