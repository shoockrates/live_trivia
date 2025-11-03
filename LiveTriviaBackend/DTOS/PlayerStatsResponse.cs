namespace live_trivia.Dtos;

public record PlayerStatsResponse
{
    public int TotalGamesPlayed { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalScore { get; set; }
    public int BestScore { get; set; }
    public double AccuracyPercentage { get; set; }
    public double AverageScore { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public List<CategoryStat> CategoryStats { get; set; } = new();
}
