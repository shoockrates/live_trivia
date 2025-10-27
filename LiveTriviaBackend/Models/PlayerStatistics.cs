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

    // Category-specific stats (store as JSON or separate table)
    public string CategoryStatsJson { get; set; } = "{}";

    // Time-based stats
    public DateTime? LastPlayedAt { get; set; }

    // Relationships
    public virtual Player Player { get; set; } = null!;
}
