using Xunit;
using Microsoft.EntityFrameworkCore;
using live_trivia.Data;
using live_trivia.Repositories;
using live_trivia;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;

public class GameRepositoryTests
{
    private async Task SeedQuestionsAsync(TriviaDbContext db, string category = "Geography", string difficulty = "Easy", int count = 5)
    {
        var questions = Enumerable.Range(1, count)
            .Select(i => new Question
            {
                Category = category,
                Difficulty = difficulty,
                Text = $"Sample Question {i}",
                CorrectAnswerIndexes = {i},
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

    [Fact]
    public async Task StartGameAsync_ShouldReturnFalse_WhenNoPlayers()
    {
        var db = GetInMemoryDb();
        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var repo = new GamesRepository(db);

        var result = await repo.StartGameAsync("12345");

        var gameFromDb = await db.Games.FirstAsync(g => g.RoomId == "12345");
        Assert.Equal(GameState.WaitingForPlayers, gameFromDb.State);
        Assert.Equal(-1, gameFromDb.CurrentQuestionIndex);
        Assert.Empty(gameFromDb.Questions);
        Assert.False(result);
    }

    [Fact]
    public async Task StartGameAsync_ShouldReturnFalse_WhenNullSettings()
    {
        // Arrange
        var db = GetInMemoryDb();

        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var repo = new GamesRepository(db);

        // Add existing players (simulate multiple players in a room)
        var player1 = new Player { Id = 1, Name = "TestPlayer1" };
        var player2 = new Player { Id = 2, Name = "TestPlayer2" };

        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        await repo.AddExistingPlayerToGameAsync(createdGame, player1);
        await repo.AddExistingPlayerToGameAsync(createdGame, player2);

        // Act
        var result = await repo.StartGameAsync("12345");

        // Assert;
        Assert.Equal(GameState.WaitingForPlayers, createdGame.State);
        Assert.Equal(-1, createdGame.CurrentQuestionIndex);
        Assert.Empty(createdGame.Questions);
        Assert.False(result); // since no GameSettings exist
    }

    [Fact]
    public async Task StartGameAsync_ShouldReturnTrue_WithPlayersAndSettings()
    {
        // Arrange
        var db = GetInMemoryDb();

        var createdGame = new Game { RoomId = "12345" };
        db.Games.Add(createdGame);
        await db.SaveChangesAsync();

        var repo = new GamesRepository(db);

        // Add existing players (simulate multiple players in a room)
        var player1 = new Player { Id = 1, Name = "TestPlayer1" };
        var player2 = new Player { Id = 2, Name = "TestPlayer2" };

        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        await repo.AddExistingPlayerToGameAsync(createdGame, player1);
        await repo.AddExistingPlayerToGameAsync(createdGame, player2);
        db.Entry(createdGame).Reload();

        // Seed questions matching settings
        await SeedQuestionsAsync(db, "Geography", "Easy", 10);

        // Add GameSettings
        var settings = new GameSettings { GameRoomId = "12345", Category = "Geography", Difficulty = "Easy", QuestionCount = 5, TimeLimitSeconds = 20 };
        db.GameSettings.Add(settings);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.StartGameAsync("12345");

        // Assert
        Assert.Equal(GameState.InProgress, createdGame.State);
        Assert.Equal(0, createdGame.CurrentQuestionIndex);
        Assert.NotNull(createdGame.StartedAt);
        Assert.Equal(settings.QuestionCount, createdGame.Questions.Count);
        Assert.True(result); // now all conditions are met
    }
}
