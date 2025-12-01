using live_trivia.Data;
using live_trivia.Dtos;
using live_trivia.Extensions;
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

        public async Task AddSync(Game game)
        {
            await _context.Games.AddAsync(game);
        }


        public async Task<Game?> GetGameAsync(
            string roomId,
            bool includePlayers = false,
            bool includeQuestions = false,
            bool includeAnswers = false)
        {
            var query = _context.Games.AsQueryable();

            if (includePlayers)
                query = query.Include(g => g.GamePlayers).ThenInclude(gp => gp.Player);

            if (includeQuestions)
                query = query.Include(g => g.Questions);

            if (includeAnswers)
                query = query.Include(g => g.PlayerAnswers);

            return await query.FirstOrDefaultAsync(g => g.RoomId == roomId);
        }



        public async Task<Game?> GetGameDetailsAsync(string roomId)
        {
            return await _context.Games
                .Include(g => g.HostPlayer)
                .Include(g => g.GamePlayers).ThenInclude(gp => gp.Player)
                .Include(g => g.Questions)
                .Include(g => g.PlayerAnswers)
                .FirstOrDefaultAsync(g => g.RoomId == roomId);
        }

        public async Task<Player?> GetPlayerByIdAsync(int playerId)
        {
            return await _context.Players
                                 .FirstOrDefaultAsync(p => p.Id == playerId);
        }

        public async Task AddGamePlayerAsync(GamePlayer player)
        {
            await _context.GamePlayers.AddAsync(player);
            await SaveChangesAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync(); // Only needed if using EF Core or similar
        }
        public async Task AddGameSettings(GameSettings settings)
        {
            _context.Add(settings);
            await SaveChangesAsync();
        }
        public async Task<GameSettings?> GetGameSettingsAsync(string roomId)
        {
            return await _context.GameSettings.FirstOrDefaultAsync(s => s.GameRoomId == roomId);
        }

        public async Task DeleteGameAsync(string roomId)
        {
            var game = await _context.Games
                .FirstOrDefaultAsync(g => g.RoomId == roomId);

            if (game != null)
            {
                _context.Games.Remove(game);
                await SaveChangesAsync(); // Call save here instead of in service
            }
        }
    }
}
