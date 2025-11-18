using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using live_trivia.Data;
using live_trivia.Services;
using live_trivia.Dtos;

namespace live_trivia.Tests.Services
{
    public class StatisticsServiceTests
    {
        private TriviaDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<TriviaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TriviaDbContext(options);
        }

        private StatisticsService CreateService(TriviaDbContext db) => new StatisticsService(db);

        [Fact]
        public async Task InitializePlayerStatisticsAsync_ShouldCreateStats_WhenNoneExist()
        {
            // Arrange
            var db = GetInMemoryDb();
            var service = CreateService(db);

            // Act
            await service.InitializePlayerStatisticsAsync(1);

            // Assert
            var stats = await db.PlayerStatistics.FirstOrDefaultAsync(ps => ps.PlayerId == 1);
            Assert.NotNull(stats);
            Assert.Equal(0, stats.TotalGamesPlayed);
            Assert.Equal(0, stats.TotalScore);
            Assert.Equal(0, stats.TotalCorrectAnswers);
        }

        [Fact]
        public async Task InitializePlayerStatisticsAsync_ShouldNotCreateDuplicate()
        {
            var db = GetInMemoryDb();
            db.PlayerStatistics.Add(new PlayerStatistics { PlayerId = 1 });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            await service.InitializePlayerStatisticsAsync(1);

            var count = await db.PlayerStatistics.CountAsync(ps => ps.PlayerId == 1);
            Assert.Equal(1, count); // no duplicate
        }

        [Fact]
        public async Task UpdateGameStatisticsAsync_ShouldUpdateOverallStats()
        {
            var db = GetInMemoryDb();
            db.PlayerStatistics.Add(new PlayerStatistics { PlayerId = 1 });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            await service.UpdateGameStatisticsAsync(1, "Geography", 50, 8, 10);

            var stats = await db.PlayerStatistics.Include(ps => ps.CategoryStatistics)
                .FirstOrDefaultAsync(ps => ps.PlayerId == 1);

            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalGamesPlayed);
            Assert.Equal(50, stats.TotalScore);
            Assert.Equal(8, stats.TotalCorrectAnswers);
            Assert.Equal(10, stats.TotalQuestionsAnswered);
            Assert.Equal(50, stats.BestScore);

            var catStat = stats.CategoryStatistics.FirstOrDefault(cs => cs.Category == "Geography");
            Assert.NotNull(catStat);
            Assert.Equal(1, catStat.GamesPlayed);
            Assert.Equal(8, catStat.CorrectAnswers);
            Assert.Equal(10, catStat.TotalQuestions);
            Assert.Equal(80, catStat.Accuracy); // 8/10 * 100
        }

        [Fact]
        public async Task UpdateGameStatisticsAsync_ShouldUpdateExistingCategoryStats()
        {
            var db = GetInMemoryDb();
            var stats = new PlayerStatistics { PlayerId = 1 };
            db.PlayerStatistics.Add(stats);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            // First game
            await service.UpdateGameStatisticsAsync(1, "History", 30, 6, 10);
            // Second game in same category
            await service.UpdateGameStatisticsAsync(1, "History", 40, 7, 10);

            stats = await db.PlayerStatistics.Include(ps => ps.CategoryStatistics)
                .FirstOrDefaultAsync(ps => ps.PlayerId == 1);

            var catStat = stats.CategoryStatistics.First(cs => cs.Category == "History");
            Assert.Equal(2, catStat.GamesPlayed);
            Assert.Equal(13, catStat.CorrectAnswers);
            Assert.Equal(20, catStat.TotalQuestions);
            Assert.Equal(65, catStat.Accuracy); // 13/20 * 100
        }

        [Fact]
        public async Task GetPlayerStatisticsAsync_ShouldReturnCorrectAggregates()
        {
            var db = GetInMemoryDb();
            var stats = new PlayerStatistics
            {
                PlayerId = 1,
                TotalGamesPlayed = 2,
                TotalQuestionsAnswered = 20,
                TotalCorrectAnswers = 15,
                TotalScore = 90,
                BestScore = 50,
                CategoryStatistics = new[]
                {
                    new CategoryStatistics
                    {
                        Category = "Science",
                        GamesPlayed = 2,
                        CorrectAnswers = 15,
                        TotalQuestions = 20,
                        Accuracy = 75
                    }
                }.ToList()
            };
            db.PlayerStatistics.Add(stats);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var response = await service.GetPlayerStatisticsAsync(1);

            Assert.Equal(2, response.TotalGamesPlayed);
            Assert.Equal(90, response.TotalScore);
            Assert.Equal(15, response.TotalCorrectAnswers);
            Assert.Equal(50, response.BestScore);
            Assert.Equal(75, response.CategoryStats.First().Accuracy);
            Assert.Equal("Science", response.CategoryStats.First().Category);
        }

        [Fact]
        public async Task GetPlayerStatisticsAsync_ShouldInitializeStats_IfNoneExist()
        {
            var db = GetInMemoryDb();
            var service = CreateService(db);

            var response = await service.GetPlayerStatisticsAsync(999);

            Assert.NotNull(response);
            Assert.Equal(0, response.TotalGamesPlayed);
            Assert.Equal(0, response.TotalScore);
            Assert.Empty(response.CategoryStats);
        }
    }
}
