namespace live_trivia.Dtos;

public record GameSettingsDto
{
    public string? Category { get; init; }
    public string? Difficulty { get; init; }
    public int QuestionCount { get; init; }
    public int TimeLimitSeconds { get; init; }
}
