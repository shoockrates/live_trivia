namespace live_trivia.Records;
public record LeaderboardResponse
{
    public List<LeaderboardEntry> Players { get; set; } = new();
    public string Filter { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}