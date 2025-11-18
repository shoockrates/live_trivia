using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using live_trivia.Data;
using live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Interfaces;
using live_trivia.Dtos;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace live_trivia.Tests.Integration
{
    public class GameIntegrationTests
    {
        private ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // Use InMemory DB
            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase($"TriviaDb_{System.Guid.NewGuid()}")
            );

            // Repositories
            services.AddScoped<GamesRepository>();
            services.AddScoped<QuestionsRepository>();

            // Services via interfaces
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IQuestionService, QuestionService>();

            return services.BuildServiceProvider();
        }

        private async Task SeedQuestionsAsync(TriviaDbContext context, string category = "Geography", string difficulty = "Easy", int count = 5)
        {
            var questions = Enumerable.Range(1, count).Select(i => new Question
            {
                Text = $"Question {i}",
                Category = category,
                Difficulty = difficulty,
                Answers = new List<string> { "A", "B", "C", "D" },
                CorrectAnswerIndexes = new List<int> { 0 },
                CreatedAt = System.DateTime.UtcNow
            }).ToList();

            await context.Questions.AddRangeAsync(questions);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Can_Create_And_Start_Game()
        {
            var provider = BuildServiceProvider();
            var context = provider.GetRequiredService<TriviaDbContext>();
            var gameService = provider.GetRequiredService<IGameService>();

            // Seed questions
            await SeedQuestionsAsync(context);

            // Create player
            var player = new Player { Name = "HostPlayer", CreatedAt = System.DateTime.UtcNow };
            context.Players.Add(player);
            await context.SaveChangesAsync();

            // Create game
            var game = await gameService.CreateGameAsync("room1", player);
            Assert.NotNull(game);
            Assert.Equal("room1", game.RoomId);

            // Update settings
            var settingsDto = new GameSettingsDto
            {
                Category = "Geography",
                Difficulty = "Easy",
                QuestionCount = 3,
                TimeLimitSeconds = 15
            };
            var settings = await gameService.UpdateGameSettingsAsync("room1", settingsDto);
            Assert.Equal("Geography", settings.Category);

            // Add player to game
            await gameService.AddExistingPlayerToGameAsync(game, player);

            // Start game
            var started = await gameService.StartGameAsync("room1");
            Assert.True(started);

            // Verify game details
            var details = await gameService.GetGameDetailsAsync("room1");
            Assert.NotNull(details);
            Assert.Equal(1, details.Players.Count);
        }
    }
}
