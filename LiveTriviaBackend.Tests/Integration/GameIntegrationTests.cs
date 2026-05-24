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
using Moq; // <--- ADDED: Needed for mocking

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
            
            // 🛑 FIX: Register the missing IActiveGamesService 🛑
            // Create a mock instance
            var mockActiveGamesService = new Mock<IActiveGamesService>();
            
            // Register the mock instance as a Singleton
            // Integration tests should not rely on the external state of IActiveGamesService,
            // so passing a mock object prevents exceptions during GameService construction.
            services.AddSingleton(mockActiveGamesService.Object);

            // Services via interfaces (Now GameService can be constructed)
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IChatService, ChatService>();

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

        private async Task<(Game game, Player player1, Player player2)> SeedChatGameAsync(
    TriviaDbContext context,
    string roomId = "chat-room")
    {
        var player1 = new Player
        {
            Name = "Martynas",
            CreatedAt = System.DateTime.UtcNow
        };

        var player2 = new Player
        {
            Name = "Player2",
            CreatedAt = System.DateTime.UtcNow
        };

        context.Players.AddRange(player1, player2);
        await context.SaveChangesAsync();

        var game = new Game
        {
            RoomId = roomId,
            State = GameState.WaitingForPlayers,
            CreatedAt = System.DateTime.UtcNow,
            GamePlayers = new List<GamePlayer>
            {
                new GamePlayer
                {
                    Game = null!,
                    Player = player1,
                    PlayerId = player1.Id
                },
                new GamePlayer
                {
                    Game = null!,
                    Player = player2,
                    PlayerId = player2.Id
                }
            }
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();

        return (game, player1, player2);
    }

        [Fact]
        public async Task Can_Create_And_Start_Game()
        {
            var provider = BuildServiceProvider();
            // Using GetRequiredService within a using block ensures scope is disposed correctly
            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

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

        [Fact]
        public async Task Chat_Can_Send_Message_In_Waiting_Room()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, _) = await SeedChatGameAsync(context);

                var result = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "Hello chat");

                Assert.NotNull(result);
                Assert.Equal("Hello chat", result.Message);
                Assert.Equal(player1.Id, result.SenderPlayerId);

                var savedMessage = await context.ChatMessages.FirstOrDefaultAsync();
                Assert.NotNull(savedMessage);
                Assert.Equal("Hello chat", savedMessage!.MessageText);
            }
        }

        [Fact]
        public async Task Chat_Is_Allowed_In_Lobby_And_After_Game_Ends_But_Blocked_During_Game()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (game, player1, _) = await SeedChatGameAsync(context);

                var lobbyMessage = await chatService.SendMessageAsync(
                    game.RoomId,
                    player1.Id,
                    player1.Name,
                    "Lobby message");

                Assert.NotNull(lobbyMessage);
                Assert.Equal("Lobby message", lobbyMessage.Message);

                game.State = GameState.InProgress;
                await context.SaveChangesAsync();

                var inProgressException = await Assert.ThrowsAsync<System.InvalidOperationException>(() =>
                    chatService.SendMessageAsync(
                        game.RoomId,
                        player1.Id,
                        player1.Name,
                        "Blocked message"));

                Assert.Equal("Chat is not available while the game is in progress.", inProgressException.Message);

                game.State = GameState.Finished;
                await context.SaveChangesAsync();

                var finishedMessage = await chatService.SendMessageAsync(
                    game.RoomId,
                    player1.Id,
                    player1.Name,
                    "Results message");

                Assert.NotNull(finishedMessage);
                Assert.Equal("Results message", finishedMessage.Message);

                var messages = await context.ChatMessages
                    .Where(m => m.GameRoomId == game.RoomId && m.DeletedAt == null)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                Assert.Equal(2, messages.Count);
                Assert.Equal("Lobby message", messages[0].MessageText);
                Assert.Equal("Results message", messages[1].MessageText);
            }
        }

        [Fact]
        public async Task Chat_Does_Not_Allow_Empty_Message()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, _) = await SeedChatGameAsync(context);

                await Assert.ThrowsAsync<System.InvalidOperationException>(() =>
                    chatService.SendMessageAsync(
                        "chat-room",
                        player1.Id,
                        player1.Name,
                        "   "));
            }
        }

        [Fact]
        public async Task Chat_Player_Can_Delete_Own_Message()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, _) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "Delete me");

                var deleted = await chatService.DeleteMessageAsync(message.Id, player1.Id);

                Assert.True(deleted);

                var dbMessage = await context.ChatMessages.FirstAsync(m => m.Id == message.Id);
                Assert.NotNull(dbMessage.DeletedAt);
            }
        }

        [Fact]
        public async Task Chat_Player_Cannot_Delete_Other_Player_Message()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, player2) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "Do not delete");

                var deleted = await chatService.DeleteMessageAsync(message.Id, player2.Id);

                Assert.False(deleted);

                var dbMessage = await context.ChatMessages.FirstAsync(m => m.Id == message.Id);
                Assert.Null(dbMessage.DeletedAt);
            }
        }

        [Fact]
        public async Task Chat_Reaction_Is_Added_To_Message()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, player2) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "React to me");

                var updatedMessage = await chatService.ToggleReactionAsync(
                    message.Id,
                    player2.Id,
                    "👍");

                Assert.NotNull(updatedMessage);
                Assert.True(updatedMessage!.Reactions.ContainsKey("👍"));
                Assert.Equal(1, updatedMessage.Reactions["👍"]);
            }
        }

        [Fact]
        public async Task Chat_Same_Reaction_Clicked_Twice_Is_Removed()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, player2) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "React twice");

                await chatService.ToggleReactionAsync(message.Id, player2.Id, "👍");

                var updatedMessage = await chatService.ToggleReactionAsync(
                    message.Id,
                    player2.Id,
                    "👍");

                Assert.NotNull(updatedMessage);
                Assert.False(updatedMessage!.Reactions.ContainsKey("👍"));
            }
        }

        [Fact]
        public async Task Chat_Different_Reaction_Replaces_Previous_Reaction()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, player2) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "Change reaction");

                await chatService.ToggleReactionAsync(message.Id, player2.Id, "👍");

                var updatedMessage = await chatService.ToggleReactionAsync(
                    message.Id,
                    player2.Id,
                    "❤️");

                Assert.NotNull(updatedMessage);
                Assert.False(updatedMessage!.Reactions.ContainsKey("👍"));
                Assert.True(updatedMessage.Reactions.ContainsKey("❤️"));
                Assert.Equal(1, updatedMessage.Reactions["❤️"]);
            }
        }

        [Fact]
        public async Task Chat_History_Does_Not_Return_Deleted_Messages()
        {
            var provider = BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                var (_, player1, _) = await SeedChatGameAsync(context);

                var message = await chatService.SendMessageAsync(
                    "chat-room",
                    player1.Id,
                    player1.Name,
                    "Hidden message");

                await chatService.DeleteMessageAsync(message.Id, player1.Id);

                var history = await chatService.GetRoomHistoryAsync("chat-room");

                Assert.Empty(history);
            }
        }
    }
}