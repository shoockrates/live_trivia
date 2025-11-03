using live_trivia.Dtos;
namespace live_trivia.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest loginRequest);
    Task<AuthResponse> RegisterAsync(RegisterRequest registerRequest);
}
