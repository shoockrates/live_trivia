using Xunit;
using Microsoft.EntityFrameworkCore;
using live_trivia.Data;
using live_trivia.Repositories;
using live_trivia;
using System.Threading.Tasks;

public class GameRepositoryTests
{
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
        db.Games.Add(new Game { RoomId = "12345" });
        await db.SaveChangesAsync();

        var repo = new GamesRepository(db);

        var result = await repo.StartGameAsync("12345");

        Assert.False(result);
    }
}
