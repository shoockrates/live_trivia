using live_trivia.Data;
using Microsoft.AspNetCore.Mvc;

namespace live_trivia.Controllers
{
    [ApiController]
    [Route("games")]
    public class GamesController : ControllerBase
    {
        private readonly TriviaDbContext _context;

        public GamesController(TriviaDbContext context)
        {
            _context = context;
        }

        [HttpPost("{roomId}")]
        public async Task<IActionResult> CreateGame(string roomId)
        {
            var game = new Game(roomId);
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return Created($"/games/{roomId}", game);
        }

        [HttpPost("{roomId}/players")]
        public async Task<IActionResult> JoinGame(string roomId, [FromQuery] string playerName)
        {
            var game = await _context.Games.FindAsync(roomId);
            if (game == null) return NotFound("Game not found");

            var player = new Player { Name = playerName };
            game.AddPlayer(player);
            await _context.SaveChangesAsync();

            return Ok(player);
        }
    }
}
