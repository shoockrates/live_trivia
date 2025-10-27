using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using live_trivia.Records;
using live_trivia.Interfaces;

namespace live_trivia.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly TriviaDbContext _context;

        public LeaderboardService(TriviaDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(int topCount = 10)
        {
            var playerStats = await _context.PlayerStatistics
                .Include(ps => ps.Player)
                .Where(ps => ps.TotalGamesPlayed > 0)
                .OrderByDescending(ps => ps.TotalScore)
                .ThenByDescending(ps => ps.TotalCorrectAnswers)
                .ThenBy(ps => ps.Player.Name)
                .Take(topCount)
                .ToListAsync();

            return playerStats.Select((ps, index) => new LeaderboardEntry
            {
                PlayerId = ps.PlayerId,
                Username = ps.Player.Name,
                TotalScore = ps.TotalScore,
                GamesPlayed = ps.TotalGamesPlayed,
                Accuracy = ps.TotalQuestionsAnswered > 0 ?
                    Math.Round((double)ps.TotalCorrectAnswers / ps.TotalQuestionsAnswered * 100, 2) : 0,
                BestScore = ps.BestScore,
                LastPlayedAt = ps.LastPlayedAt,
                Rank = index + 1
            }).ToList();
        }

        public async Task<List<LeaderboardEntry>> GetTopPlayersByCategoryAsync(string category, int topCount = 10)
        {
            var categoryStats = await _context.CategoryStatistics
                .Include(cs => cs.PlayerStatistics)
                    .ThenInclude(ps => ps.Player)
                .Where(cs => cs.Category == category && cs.GamesPlayed > 0)
                .OrderByDescending(cs => cs.Accuracy)
                .ThenByDescending(cs => cs.GamesPlayed)
                .ThenBy(cs => cs.PlayerStatistics.Player.Name)
                .Take(topCount)
                .ToListAsync();

            return categoryStats.Select((cs, index) => new LeaderboardEntry
            {
                PlayerId = cs.PlayerStatistics.PlayerId,
                Username = cs.PlayerStatistics.Player.Name,
                TotalScore = cs.PlayerStatistics.TotalScore,
                GamesPlayed = cs.GamesPlayed,
                Accuracy = cs.Accuracy,
                BestScore = cs.PlayerStatistics.BestScore,
                LastPlayedAt = cs.PlayerStatistics.LastPlayedAt,
                Category = category,
                Rank = index + 1
            }).ToList();
        }

        public async Task<List<string>> GetAvailableCategoriesAsync()
        {
            return await _context.CategoryStatistics
                .Select(cs => cs.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
