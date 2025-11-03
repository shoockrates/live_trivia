namespace live_trivia.Dtos;
public record RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}