using Xunit;
using Microsoft.EntityFrameworkCore;
using live_trivia.Data;
using live_trivia.Services;
using live_trivia;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace live_trivia.Tests.ServiceTests
{
    public class LeaderboardServiceTests
    {
        private TriviaDbContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<TriviaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new TriviaDbContext(options);
        }

        private LeaderboardService CreateService(TriviaDbContext db)
        {
            return new LeaderboardService(db);
        }

        [Fact]
        public async Task GetTopPlayersAsync_ReturnsEmptyList_WhenNoPlayers()
        {
            var db = GetInMemoryDb();
            var service = CreateService(db);

            var result = await service.GetTopPlayersAsync(10);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTopPlayersAsync_ReturnsTopPlayers_OrderedByScore()
        {
            var db = GetInMemoryDb();
            
            var player1 = new Player { Id = 1, Name = "Player1" };
            var player2 = new Player { Id = 2, Name = "Player2" };
            var player3 = new Player { Id = 3, Name = "Player3" };
            db.Players.AddRange(player1, player2, player3);
            await db.SaveChangesAsync();

            var stats1 = new PlayerStatistics
            {
                PlayerId = 1,
                TotalScore = 1000,
                TotalGamesPlayed = 5,
                TotalCorrectAnswers = 20,
                TotalQuestionsAnswered = 25,
                BestScore = 250
            };
            var stats2 = new PlayerStatistics
            {
                PlayerId = 2,
                TotalScore = 2000,
                TotalGamesPlayed = 10,
                TotalCorrectAnswers = 40,
                TotalQuestionsAnswered = 50,
                BestScore = 300
            };
            var stats3 = new PlayerStatistics
            {
                PlayerId = 3,
                TotalScore = 500,
                TotalGamesPlayed = 2,
                TotalCorrectAnswers = 10,
                TotalQuestionsAnswered = 15,
                BestScore = 200
            };
            db.PlayerStatistics.AddRange(stats1, stats2, stats3);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.GetTopPlayersAsync(10);

            Assert.Equal(3, result.Count);
            Assert.Equal(2, result[0].PlayerId); // Highest score
            Assert.Equal(1, result[1].PlayerId);
            Assert.Equal(3, result[2].PlayerId);
            Assert.Equal(1, result[0].Rank);
            Assert.Equal(2, result[1].Rank);
            Assert.Equal(3, result[2].Rank);
        }

        [Fact]
        public async Task GetTopPlayersAsync_RespectsTopCount()
        {
            var db = GetInMemoryDb();
            
            for (int i = 1; i <= 15; i++)
            {
                var player = new Player { Id = i, Name = $"Player{i}" };
                db.Players.Add(player);
                await db.SaveChangesAsync();

                var stats = new PlayerStatistics
                {
                    PlayerId = i,
                    TotalScore = 1000 - i,
                    TotalGamesPlayed = 1,
                    TotalCorrectAnswers = 5,
                    TotalQuestionsAnswered = 10
                };
                db.PlayerStatistics.Add(stats);
                await db.SaveChangesAsync();
            }

            var service = CreateService(db);

            var result = await service.GetTopPlayersAsync(5);

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetTopPlayersAsync_CalculatesAccuracyCorrectly()
        {
            var db = GetInMemoryDb();
            
            var player = new Player { Id = 1, Name = "Player1" };
            db.Players.Add(player);
            await db.SaveChangesAsync();

            var stats = new PlayerStatistics
            {
                PlayerId = 1,
                TotalScore = 1000,
                TotalGamesPlayed = 1,
                TotalCorrectAnswers = 8,
                TotalQuestionsAnswered = 10
            };
            db.PlayerStatistics.Add(stats);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.GetTopPlayersAsync(10);

            Assert.Single(result);
            Assert.Equal(80.0, result[0].Accuracy);
        }

        [Fact]
        public async Task GetTopPlayersByCategoryAsync_ReturnsEmptyList_WhenNoPlayersInCategory()
        {
            var db = GetInMemoryDb();
            var service = CreateService(db);

            var result = await service.GetTopPlayersByCategoryAsync("Geography", 10);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTopPlayersByCategoryAsync_ReturnsTopPlayers_ForCategory()
        {
            var db = GetInMemoryDb();
            
            var player1 = new Player { Id = 1, Name = "Player1" };
            var player2 = new Player { Id = 2, Name = "Player2" };
            db.Players.AddRange(player1, player2);
            await db.SaveChangesAsync();

            var stats1 = new PlayerStatistics
            {
                PlayerId = 1,
                TotalScore = 1000,
                TotalGamesPlayed = 5,
                BestScore = 250,
                LastPlayedAt = DateTime.UtcNow
            };
            var stats2 = new PlayerStatistics
            {
                PlayerId = 2,
                TotalScore = 2000,
                TotalGamesPlayed = 10,
                BestScore = 300,
                LastPlayedAt = DateTime.UtcNow
            };
            db.PlayerStatistics.AddRange(stats1, stats2);
            await db.SaveChangesAsync();

            var catStats1 = new CategoryStatistics
            {
                PlayerStatisticsId = stats1.Id,
                Category = "Geography",
                GamesPlayed = 3,
                Accuracy = 80.0,
                CorrectAnswers = 12,
                TotalQuestions = 15
            };
            var catStats2 = new CategoryStatistics
            {
                PlayerStatisticsId = stats2.Id,
                Category = "Geography",
                GamesPlayed = 5,
                Accuracy = 90.0,
                CorrectAnswers = 18,
                TotalQuestions = 20
            };
            db.CategoryStatistics.AddRange(catStats1, catStats2);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.GetTopPlayersByCategoryAsync("Geography", 10);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].PlayerId); // Higher accuracy
            Assert.Equal(1, result[1].PlayerId);
            Assert.Equal("Geography", result[0].Category);
            Assert.Equal("Geography", result[1].Category);
        }

        [Fact]
        public async Task GetAvailableCategoriesAsync_ReturnsEmptyList_WhenNoCategories()
        {
            var db = GetInMemoryDb();
            var service = CreateService(db);

            var result = await service.GetAvailableCategoriesAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableCategoriesAsync_ReturnsDistinctCategories_Ordered()
        {
            var db = GetInMemoryDb();
            
            var player = new Player { Id = 1, Name = "Player1" };
            db.Players.Add(player);
            await db.SaveChangesAsync();

            var stats = new PlayerStatistics { PlayerId = 1 };
            db.PlayerStatistics.Add(stats);
            await db.SaveChangesAsync();

            var catStats1 = new CategoryStatistics { PlayerStatisticsId = stats.Id, Category = "History" };
            var catStats2 = new CategoryStatistics { PlayerStatisticsId = stats.Id, Category = "Geography" };
            var catStats3 = new CategoryStatistics { PlayerStatisticsId = stats.Id, Category = "Science" };
            db.CategoryStatistics.AddRange(catStats1, catStats2, catStats3);
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var result = await service.GetAvailableCategoriesAsync();

            Assert.Equal(3, result.Count);
            Assert.Equal("Geography", result[0]);
            Assert.Equal("History", result[1]);
            Assert.Equal("Science", result[2]);
        }
    }
}
