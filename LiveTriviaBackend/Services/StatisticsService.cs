using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using live_trivia.Utilities;

namespace live_trivia.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly TriviaDbContext _context;
        private readonly CollectionProjector<CategoryStatistics> _categoryProjector = new();

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
                    CreatedAt = DateTime.UtcNow
                };

                _context.PlayerStatistics.Add(stats);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateGameStatisticsAsync(int playerId, string category, int score, int correctAnswers, int totalQuestions)
        {
            var stats = await _context.PlayerStatistics
                .Include(ps => ps.CategoryStatistics)
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);

            if (stats == null)
            {
                await InitializePlayerStatisticsAsync(playerId);
                stats = await _context.PlayerStatistics
                    .Include(ps => ps.CategoryStatistics)
                    .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            }

            if (stats == null)
            {
                throw new InvalidOperationException($"Failed to initialize statistics for player {playerId}");
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
            UpdateCategoryStatistics(stats, category, correctAnswers, totalQuestions);

            await _context.SaveChangesAsync();
        }

        private void UpdateCategoryStatistics(PlayerStatistics stats, string category, int correctAnswers, int totalQuestions)
        {
            var categoryStat = stats.CategoryStatistics
                .FirstOrDefault(cs => cs.Category == category);

            if (categoryStat != null)
            {
                categoryStat.GamesPlayed++;
                categoryStat.CorrectAnswers += correctAnswers;
                categoryStat.TotalQuestions += totalQuestions;
                categoryStat.Accuracy = categoryStat.TotalQuestions > 0 ?
                    Math.Round((double)categoryStat.CorrectAnswers / categoryStat.TotalQuestions * 100, 2) : 0;
                categoryStat.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                categoryStat = new CategoryStatistics
                {
                    PlayerStatisticsId = stats.Id,
                    Category = category,
                    GamesPlayed = 1,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = totalQuestions,
                    Accuracy = totalQuestions > 0 ? Math.Round((double)correctAnswers / totalQuestions * 100, 2) : 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CategoryStatistics.Add(categoryStat);
            }
        }

        public async Task<PlayerStatsResponse> GetPlayerStatisticsAsync(int playerId)
        {
            var stats = await _context.PlayerStatistics
                .Include(ps => ps.CategoryStatistics)
                .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);

            if (stats == null)
            {
                await InitializePlayerStatisticsAsync(playerId);
                stats = await _context.PlayerStatistics
                    .Include(ps => ps.CategoryStatistics)
                    .FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            }

            if (stats == null)
            {
                return new PlayerStatsResponse();
            }

            var categoryStats = _categoryProjector.Project<CategoryStat, int>(
                stats.CategoryStatistics,
                cs => cs.GamesPlayed,
                cs => new CategoryStat
                {
                    Category = cs.Category,
                    GamesPlayed = cs.GamesPlayed,
                    CorrectAnswers = cs.CorrectAnswers,
                    TotalQuestions = cs.TotalQuestions,
                    Accuracy = cs.Accuracy
                })
                .ToList();

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
