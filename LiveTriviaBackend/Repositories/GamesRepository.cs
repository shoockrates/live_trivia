using live_trivia.Data;
using Microsoft.EntityFrameworkCore;

namespace live_trivia.Repositories
{
    public class GamesRepository
    {
        private readonly TriviaDbContext _context;

        public GamesRepository(TriviaDbContext context)
        {
            _context = context;
        }

        public async Task<Game> CreateGameAsync(string roomId)
        {
            var game = new Game(roomId);
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<Game?> GetGameAsync(string roomId)
        {
            return await _context.Games
                .Include(g => g.GamePlayers)
                .ThenInclude(gp => gp.Player)
                .FirstOrDefaultAsync(g => g.RoomId == roomId);
        }

        public async Task<Player> AddPlayerAsync(Game game, string playerName)
        {
            var player = new Player { Name = playerName };
            game.AddPlayer(player);
            await _context.SaveChangesAsync();
            return player;
        }
    }
}
