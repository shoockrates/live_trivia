using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using live_trivia.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace live_trivia.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest loginRequest);
    Task<AuthResponse> RegisterAsync(RegisterRequest registerRequest);
}