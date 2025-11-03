namespace LiveTrivia.Dtos;

public record LeaderboardEntry
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public double Accuracy { get; set; }
    public int BestScore { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public string? Category { get; set; }
    public int Rank { get; set; }
}
