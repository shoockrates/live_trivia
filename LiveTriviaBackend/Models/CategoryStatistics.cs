namespace live_trivia;

public class CategoryStatistics : BaseEntity
{
    public int Id { get; set; }
    public int PlayerStatisticsId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double Accuracy { get; set; }

    // Relationships
    public virtual PlayerStatistics PlayerStatistics { get; set; } = null!;
}
