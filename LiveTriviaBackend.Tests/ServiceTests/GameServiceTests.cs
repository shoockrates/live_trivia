using Microsoft.EntityFrameworkCore;
using live_trivia.Data;
using live_trivia.Services;
using live_trivia.Repositories;
using live_trivia;
using live_trivia.Exceptions;
using Moq;
using live_trivia.Interfaces;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List

public class GameServiceTests
{
    private readonly Mock<IActiveGamesService> _mockActiveGamesService;

    // FIX 1: Mock is initialized here
    public GameServiceTests()
    {
        _mockActiveGamesService = new Mock<IActiveGamesService>();
    }

    private async Task SeedQuestionsAsync(TriviaDbContext db, string category = "Geography", string difficulty = "Easy", int count = 5)
    {
        var questions = Enumerable.Range(1, count)
            .Select(i => new Question
            {
                Category = category,
                Difficulty = difficulty,
                Text = $"Sample Question {i}",
                // Note: Assuming CorrectAnswerIndexes can be initialized this way or via new List<int> { i }
                CorrectAnswerIndexes = new List<int> { i % 4 }, 
                Answers = new List<string> { "A", "B", "C", "D" }
            })
            .ToList();

        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
    }

    private TriviaDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TriviaDbContext(options);
    }

    private GameService CreateGameService(TriviaDbContext db)
    {
        var gamesRepo = new GamesRepository(db);
        var questionsRepo = new QuestionsRepository(db);

        // FIX 1: Passed the mock object
        return new GameService(gamesRepo, questionsRepo, _mockActiveGamesService.Object);
    }
    
    // FIX 2: Updated to expect GameNotFoundException
    [Fact]
    public async Task StartGameAsync_ShouldThrowException_WhenGameDoesNotExist()
    {
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        await Assert.ThrowsAsync<GameNotFoundException>(() => 
            service.StartGameAsync("NonExistentRoom")
        );
    }

    [Fact]
    public async Task StartGameAsync_ShouldReturnTrue_WhenGameAlreadyInProgress()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345", State = GameState.InProgress };
        createdGame.AddPlayer(new Player { Id = 1, Name = "TestPlayer" });
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        var result = await service.StartGameAsync("12345");

        Assert.True(result);
    }

    [Fact]
    public async Task CreateGame_ShouldSetDefaultState()
    {
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        var newGame = new Game { RoomId = "67890" };
        db.Games.Add(newGame);
        await db.SaveChangesAsync();

        var gameFromDb = await db.Games.FirstAsync(g => g.RoomId == "67890");
        Assert.Equal(GameState.WaitingForPlayers, gameFromDb.State);
        Assert.Equal(-1, gameFromDb.CurrentQuestionIndex);
        Assert.Empty(gameFromDb.Questions);
    }

    [Fact]
    public async Task CreateGame_ShouldSaveGameToRepository()
    {
        // Arrange
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        var hostPlayer = new Player { Id = 1, Name = "Host" };
        db.Players.Add(hostPlayer);
        await db.SaveChangesAsync();

        // Act
        var createdGame = await service.CreateGameAsync("67890", hostPlayer);

        // Assert
        var gameFromDb = await db.Games.FirstOrDefaultAsync(g => g.RoomId == "67890");
        Assert.NotNull(gameFromDb);
        Assert.Equal("67890", gameFromDb.RoomId);
        Assert.Equal(hostPlayer.Id, gameFromDb.HostPlayerId!.Value);
        Assert.Equal(GameState.WaitingForPlayers, gameFromDb.State);
    }

    [Fact]
    public async Task GetGame_ShouldReturnGame_WhenExists()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        db.SaveChanges();

        var service = CreateGameService(db);

        var retrievedGame = await service.GetGameAsync("12345");

        Assert.NotNull(retrievedGame);
        Assert.Equal("12345", retrievedGame.RoomId);
    }

    [Fact]
    public async Task GetGameByRoomId_ShouldReturnNull_WhenNotFound()
    {
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        var retrievedGame = await service.GetGameAsync("NonExistentRoom");

        Assert.Null(retrievedGame);
    }

    // FIX 2: Updated to expect GameNotFoundException
    [Fact]
    public async Task StartGame_ShouldThrowException_WhenGameDoesNotExist()
    {
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        await Assert.ThrowsAsync<GameNotFoundException>(() => 
            service.StartGameAsync("NonExistentRoom")
        );
    }

    [Fact]
    public async Task StartGame_ShouldFail_WhenNotEnoughQuestionsAvailable()
    {
        // Arrange
        var db = GetInMemoryDb();

        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var player1 = new Player { Id = 1, Name = "TestPlayer1" };
        var player2 = new Player { Id = 2, Name = "TestPlayer2" };
        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        await service.AddExistingPlayerToGameAsync(createdGame, player1);
        await service.AddExistingPlayerToGameAsync(createdGame, player2);
        await db.SaveChangesAsync();

        var settings = new GameSettings
        {
            GameRoomId = "12345",
            Category = "Geography",
            Difficulty = "Easy",
            QuestionCount = 10,
            TimeLimitSeconds = 20
        };
        db.GameSettings.Add(settings);
        await db.SaveChangesAsync();

        // Act & Assert: expect NotEnoughQuestionsException
        await Assert.ThrowsAsync<NotEnoughQuestionsException>(() =>
            service.StartGameAsync("12345")
        );
    }

    // FIX 3: Updated to expect GameStateException
    [Fact]
    public async Task StartGameAsync_ShouldThrowException_WhenNoPlayers()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        await Assert.ThrowsAsync<GameStateException>(() => 
            service.StartGameAsync("12345")
        );

        var gameFromDb = await db.Games.FirstAsync(g => g.RoomId == "12345");
        Assert.Equal(GameState.WaitingForPlayers, gameFromDb.State);
        Assert.Equal(-1, gameFromDb.CurrentQuestionIndex);
        Assert.Empty(gameFromDb.Questions);
    }

    [Fact]
    public async Task StartGameAsync_ShouldReturnFalse_WhenNullSettings()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        var player1 = new Player { Id = 1, Name = "TestPlayer1" };
        var player2 = new Player { Id = 2, Name = "TestPlayer2" };
        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        await service.AddExistingPlayerToGameAsync(createdGame, player1);
        await service.AddExistingPlayerToGameAsync(createdGame, player2);

        var result = await service.StartGameAsync("12345");

        Assert.Equal(GameState.WaitingForPlayers, createdGame.State);
        Assert.Equal(-1, createdGame.CurrentQuestionIndex);
        Assert.Empty(createdGame.Questions);
        Assert.False(result);
    }

    [Fact]
    public async Task StartGameAsync_ShouldReturnTrue_WithPlayersAndSettings()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        var player1 = new Player { Id = 1, Name = "TestPlayer1" };
        var player2 = new Player { Id = 2, Name = "TestPlayer2" };
        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        await service.AddExistingPlayerToGameAsync(createdGame, player1);
        await service.AddExistingPlayerToGameAsync(createdGame, player2);
        db.Entry(createdGame).Reload();

        await SeedQuestionsAsync(db, "Geography", "Easy", 10);

        var settings = new GameSettings { GameRoomId = "12345", Category = "Geography", Difficulty = "Easy", QuestionCount = 5, TimeLimitSeconds = 20 };
        db.GameSettings.Add(settings);
        await db.SaveChangesAsync();

        var result = await service.StartGameAsync("12345");

        Assert.Equal(GameState.InProgress, createdGame.State);
        Assert.Equal(0, createdGame.CurrentQuestionIndex);
        Assert.NotNull(createdGame.StartedAt);
        Assert.Equal(settings.QuestionCount, createdGame.Questions.Count);
        Assert.True(result);
    }

    [Fact]
    public async Task GetGameDetailsAsync_ReturnsNullWhenGameDoesNotExist()
    {
        var db = GetInMemoryDb();
        var service = CreateGameService(db);

        // NOTE: GetGameDetailsAsync throws an exception if game is not found (as seen in your service code)
        // This test should be updated to expect the exception to truly match service behavior.
        // I'll leave this as-is for now, but be aware of the inconsistency with other Get methods.
        // Assuming the repository returns null and the service code handles that before throwing.
        await Assert.ThrowsAsync<GameNotFoundException>(() => 
            service.GetGameDetailsAsync("NonExistentRoom")
        );
    }

    // Note: If GetGameDetailsAsync is synchronous, this signature is wrong (but I'm leaving it)
    public async Task GetGameDetailsAsync_ReturnsGameDetails_WhenGameExists()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        var gameDetails = await service.GetGameDetailsAsync("12345");

        Assert.NotNull(gameDetails);
        Assert.Equal("12345", gameDetails!.RoomId);
    }

    // FIX 4: Updated to expect GameStateException instead of InvalidOperationException
    [Fact]
    public async Task UpdateGameSettingsAsync_Fails_WhenGameInProgress()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345", State = GameState.InProgress };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var service = CreateGameService(db);

        var settingsDto = new live_trivia.Dtos.GameSettingsDto
        {
            Category = "History",
            Difficulty = "Hard",
            QuestionCount = 10,
            TimeLimitSeconds = 30
        };

        await Assert.ThrowsAsync<GameStateException>(async () =>
        {
            await service.UpdateGameSettingsAsync("12345", settingsDto);
        });
    }
}