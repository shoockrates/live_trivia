using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using live_trivia.Data;
using live_trivia.Services;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using live_trivia;
using System.Security.Claims;
using System.Linq; // Added for First() in GenerateJwtToken_ShouldIncludeClaims
using live_trivia.Exceptions; // ADDED THIS USING for the specific exception

public class AuthServiceTests
{
    private TriviaDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TriviaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TriviaDbContext(options);
    }

    private IAuthService CreateAuthService(TriviaDbContext db)
    {
        // Minimal in-memory configuration for JWT
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "thisisaverylongsupersecretkeythatisusedforjwt"},
            {"Jwt:Issuer", "live-trivia"},
            {"Jwt:Audience", "live-trivia-users"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();


        return new AuthService(db, configuration);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndPlayer()
    {
        var db = GetInMemoryDb();
        var authService = CreateAuthService(db);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var response = await authService.RegisterAsync(request);

        Assert.NotNull(response);
        Assert.Equal("testuser", response.Username);
        Assert.NotNull(response.Token);
        Assert.True(response.PlayerId > 0);

        var userInDb = await db.Users.Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(userInDb);
        Assert.NotNull(userInDb.Player);
        Assert.Equal(userInDb.PlayerId, userInDb.Player.Id);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameExists()
    {
        var db = GetInMemoryDb();
        var authService = CreateAuthService(db);

        var existingPlayer = new Player { Name = "testuser" };
        db.Players.Add(existingPlayer);
        await db.SaveChangesAsync();

        var existingUser = new User
        {
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            PlayerId = existingPlayer.Id
        };
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "testuser",
            Password = "newpass"
        };

        // FIX APPLIED HERE:
        // Changed Assert.ThrowsAsync<Exception> to Assert.ThrowsAsync<UsernameAlreadyExistsException>
        await Assert.ThrowsAsync<UsernameAlreadyExistsException>(async () =>
        {
            await authService.RegisterAsync(request);
        });
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        var db = GetInMemoryDb();
        var authService = CreateAuthService(db);

        // Seed player + user
        var player = new Player { Name = "loginuser", Score = 0 };
        db.Players.Add(player);
        await db.SaveChangesAsync();

        var user = new User
        {
            Username = "loginuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            PlayerId = player.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "loginuser",
            Password = "password123"
        };

        var response = await authService.LoginAsync(request);

        Assert.NotNull(response);
        Assert.Equal("loginuser", response.Username);
        Assert.Equal(player.Id, response.PlayerId);
        Assert.NotNull(response.Token);
    }

    [Theory]
    [InlineData("wronguser", "password123")]
    [InlineData("loginuser", "wrongpassword")]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenInvalidCredentials(string username, string password)
    {
        var db = GetInMemoryDb();
        var authService = CreateAuthService(db);

        var player = new Player { Name = "loginuser", Score = 0 };
        db.Players.Add(player);
        await db.SaveChangesAsync();

        var user = new User
        {
            Username = "loginuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            PlayerId = player.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await authService.LoginAsync(request);
        });
    }

    [Fact]
    public async Task GenerateJwtToken_ShouldIncludeClaims()
    {
        var db = GetInMemoryDb();
        var authService = CreateAuthService(db);

        var player = new Player { Name = "jwtplayer" };
        db.Players.Add(player);
        await db.SaveChangesAsync();

        var user = new User
        {
            Username = "jwtuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            PlayerId = player.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = await authService.LoginAsync(new LoginRequest
        {
            Username = "jwtuser",
            Password = "password"
        });

        // Check that token is a valid JWT
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(response.Token);

        Assert.Equal("jwtuser", token.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(user.PlayerId.ToString(), token.Claims.First(c => c.Type == "playerId").Value);
        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
    }
}