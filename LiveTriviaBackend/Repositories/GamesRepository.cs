using live_trivia.Data;
using live_trivia.Dtos;
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

        public async Task<Game> CreateGameAsync(string roomId, Player hostPlayer)
        {
            // make sure EF is tracking the player entity
            _context.Attach(hostPlayer);

            var game = new Game(roomId)
            {
                HostPlayer = hostPlayer,
                HostPlayerId = hostPlayer.Id,
                CreatedAt = DateTime.UtcNow
            };
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

        public async Task<GameDetailsDto?> GetGameDetailsAsync(string roomId)
        {
            var game = await _context.Games
                .Include(g => g.HostPlayer)
                .Include(g => g.GamePlayers)
                    .ThenInclude(gp => gp.Player)
                .Include(g => g.Questions)
                .Include(g => g.PlayerAnswers) // load all, filter in memory
                .FirstOrDefaultAsync(g => g.RoomId == roomId);

            if (game == null) return null;

            var questionsList = game.Questions.ToList();
            var currentQuestion = game.CurrentQuestionIndex >= 0 && game.CurrentQuestionIndex < questionsList.Count
                ? questionsList[game.CurrentQuestionIndex]
                : null;

            var currentAnswers = game.PlayerAnswers
                .Where(pa => pa.QuestionId == currentQuestion?.Id)
                .ToList();

            return new GameDetailsDto
            {
                CreatedAt = game.CreatedAt,
                StartedAt = game.StartedAt,
                HostPlayerId = game.HostPlayerId,
                RoomId = game.RoomId,
                State = game.State.ToString(),
                CurrentQuestionText = currentQuestion?.Text,
                CurrentQuestionAnswers = currentQuestion?.Answers,
                CurrentQuestionIndex = game.CurrentQuestionIndex,
                TotalQuestions = questionsList.Count,
                Players = game.GamePlayers.Select(gp => new GamePlayerDto
                {
                    PlayerId = gp.PlayerId,
                    Name = gp.Player.Name,
                    CurrentScore = gp.Player.Score,
                    HasSubmittedAnswer = currentAnswers.Any(pa => pa.PlayerId == gp.PlayerId)
                }).ToList()
            };
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
        public async Task<bool> StartGameAsync(string roomId)
        {
            var game = await _context.Games
                .Include(g => g.GamePlayers)
                .Include(g => g.Questions)
                .FirstOrDefaultAsync(g => g.RoomId == roomId);

            if (game == null)
                return false;

            if (game.State == GameState.InProgress)
                return true; // already started

            if (game.GamePlayers.Count < 1 || game.Questions.Count == 0)
                return false; // cannot start empty game

            game.State = GameState.InProgress;
            game.StartedAt = DateTime.UtcNow;
            game.CurrentQuestionIndex = 0;

            _context.Games.Update(game);
            await _context.SaveChangesAsync();

            return true;
        }


    }
}
