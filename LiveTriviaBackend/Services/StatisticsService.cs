using System.Text.Json;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using live_trivia.Records;

namespace live_trivia.Services
{
    public interface IStatisticsService
    {
        Task UpdateGameStatisticsAsync(int playerId, string category, int score, int correctAnswers, int totalQuestions);
        Task<PlayerStatsResponse> GetPlayerStatisticsAsync(int playerId);
        Task InitializePlayerStatisticsAsync(int playerId);
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly TriviaDbContext _context;

        public StatisticsService(TriviaDbContext context)
        {
            _context = context;
        }

        public async Task InitializePlayerStatisticsAsync(int playerId)
        {
            var existingStats = await _context.PlayerStatistics
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);

            if (existingStats == null)
            {
                var stats = new PlayerStatistics
                {
                    PlayerId = playerId,
                    TotalGamesPlayed = 0,
                    TotalQuestionsAnswered = 0,
                    TotalCorrectAnswers = 0,
                    TotalScore = 0,
                    BestScore = 0,
                    CategoryStatsJson = "{}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PlayerStatistics.Add(stats);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateGameStatisticsAsync(int playerId, string category, int score, int correctAnswers, int totalQuestions)
        {
            var stats = await _context.PlayerStatistics
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);

            if (stats == null)
            {
                await InitializePlayerStatisticsAsync(playerId);
                stats = await _context.PlayerStatistics
                    .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            }

            // Update overall statistics
            stats.TotalGamesPlayed++;
            stats.TotalQuestionsAnswered += totalQuestions;
            stats.TotalCorrectAnswers += correctAnswers;
            stats.TotalScore += score;

            if (score > stats.BestScore)
                stats.BestScore = score;

            stats.LastPlayedAt = DateTime.UtcNow;
            stats.UpdatedAt = DateTime.UtcNow;

            // Update category-specific statistics
            await UpdateCategoryStatisticsAsync(stats, category, correctAnswers, totalQuestions);

            await _context.SaveChangesAsync();
        }

        private async Task UpdateCategoryStatisticsAsync(PlayerStatistics stats, string category, int correctAnswers, int totalQuestions)
        {
            var categoryStats = string.IsNullOrEmpty(stats.CategoryStatsJson)
                ? new Dictionary<string, CategoryStat>()
                : JsonSerializer.Deserialize<Dictionary<string, CategoryStat>>(stats.CategoryStatsJson) ?? new Dictionary<string, CategoryStat>();

            if (categoryStats.ContainsKey(category))
            {
                var existing = categoryStats[category];
                existing.GamesPlayed++;
                existing.CorrectAnswers += correctAnswers;
                existing.TotalQuestions += totalQuestions;
                existing.Accuracy = existing.TotalQuestions > 0 ?
                    Math.Round((double)existing.CorrectAnswers / existing.TotalQuestions * 100, 2) : 0;
            }
            else
            {
                categoryStats[category] = new CategoryStat
                {
                    Category = category,
                    GamesPlayed = 1,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = totalQuestions,
                    Accuracy = totalQuestions > 0 ? Math.Round((double)correctAnswers / totalQuestions * 100, 2) : 0
                };
            }

            stats.CategoryStatsJson = JsonSerializer.Serialize(categoryStats);
        }

        public async Task<PlayerStatsResponse> GetPlayerStatisticsAsync(int playerId)
        {
            var stats = await _context.PlayerStatistics
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);

            if (stats == null)
            {
                await InitializePlayerStatisticsAsync(playerId);
                stats = await _context.PlayerStatistics
                    .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            }

            var categoryStats = string.IsNullOrEmpty(stats.CategoryStatsJson)
                ? new List<CategoryStat>()
                : JsonSerializer.Deserialize<Dictionary<string, CategoryStat>>(stats.CategoryStatsJson)?
                      .Values.OrderByDescending(cs => cs.GamesPlayed).ToList() ?? new List<CategoryStat>();

            return new PlayerStatsResponse
            {
                TotalGamesPlayed = stats.TotalGamesPlayed,
                TotalQuestionsAnswered = stats.TotalQuestionsAnswered,
                TotalCorrectAnswers = stats.TotalCorrectAnswers,
                TotalScore = stats.TotalScore,
                BestScore = stats.BestScore,
                AccuracyPercentage = stats.TotalQuestionsAnswered > 0 ?
                    Math.Round((double)stats.TotalCorrectAnswers / stats.TotalQuestionsAnswered * 100, 2) : 0,
                AverageScore = stats.TotalGamesPlayed > 0 ?
                    Math.Round((double)stats.TotalScore / stats.TotalGamesPlayed, 2) : 0,
                LastPlayedAt = stats.LastPlayedAt,
                CategoryStats = categoryStats
            };
        }
    }
}
