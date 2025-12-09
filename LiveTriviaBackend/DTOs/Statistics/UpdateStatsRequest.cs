namespace live_trivia.Dtos;

public record UpdateStatsRequest
{
    public string Category { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
}