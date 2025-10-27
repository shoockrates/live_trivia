namespace live_trivia;

public class PlayerStatistics : BaseEntity
{
    public int Id { get; set; }
    public int PlayerId { get; set; }

    // Overall stats
    public int TotalGamesPlayed { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalScore { get; set; }
    public int BestScore { get; set; }

    public double AccuracyPercentage => TotalQuestionsAnswered > 0 ?
        Math.Round((double)TotalCorrectAnswers / TotalQuestionsAnswered * 100, 2) : 0;

    // Time-based stats
    public DateTime? LastPlayedAt { get; set; }

    // Relationships
    public virtual Player Player { get; set; } = null!;
    public virtual ICollection<CategoryStatistics> CategoryStatistics { get; set; } = new List<CategoryStatistics>();
}
