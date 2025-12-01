namespace live_trivia.Dtos;
public record GamePlayerDto
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int CurrentScore { get; init; }
    public bool HasSubmittedAnswer { get; init; } = false;
    public int Score { get; set; }
}
