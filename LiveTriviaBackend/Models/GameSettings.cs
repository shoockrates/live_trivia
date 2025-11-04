namespace live_trivia;

public class GameSettings
{
    public int Id { get; set; }
    public string GameRoomId { get; set; } = string.Empty;

    public string? Category { get; set; }
    public string? Difficulty { get; set; }
    public int QuestionCount { get; set; } = 10;
    public int TimeLimitSeconds { get; set; } = 15;
    public virtual Game Game { get; set; } = null!;
}
