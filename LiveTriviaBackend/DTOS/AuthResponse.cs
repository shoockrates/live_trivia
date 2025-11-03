namespace LiveTrivia.Dtos;
public record AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? PlayerId { get; set; }
}
