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

        public async Task<Player?> GetPlayerByIdAsync(int playerId)
        {
            return await _context.Players
                                 .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        public async Task AddExistingPlayerToGameAsync(Game game, Player player)
        {
            if (game == null || player == null)
            {
                throw new ArgumentNullException("Game and Player must not be null.");
            }

            var gamePlayer = new GamePlayer
            {
                GameRoomId = game.RoomId,
                PlayerId = player.Id,
            };

            _context.GamePlayers.Add(gamePlayer);
            await _context.SaveChangesAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync(); // Only needed if using EF Core or similar
        }

    }
}
