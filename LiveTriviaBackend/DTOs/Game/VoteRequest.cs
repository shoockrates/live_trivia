namespace live_trivia.Dtos;

public record VoteRequest
{
    public string Category { get; init; } = string.Empty;
}