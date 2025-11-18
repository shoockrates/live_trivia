using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using live_trivia.Data;
using live_trivia.Interfaces;
using live_trivia.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace live_trivia.Services;

public class AuthService : IAuthService
{
    private readonly TriviaDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(TriviaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new Exceptions.UsernameAlreadyExistsException(request.Username);

        // Create player profile
        var player = new Player { Name = request.Username, Score = 0 };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        // Create user
        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PlayerId = player.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            PlayerId = user.PlayerId
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Player)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            Username = user.Username,
            PlayerId = user.PlayerId
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "live-trivia";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "live-trivia-users";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Validate that PlayerId exists
        if (user.PlayerId == null)
        {
            throw new InvalidOperationException($"User {user.Username} has no associated PlayerId");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("playerId", user.PlayerId.Value.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            jwtIssuer,
            jwtAudience,
            claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

