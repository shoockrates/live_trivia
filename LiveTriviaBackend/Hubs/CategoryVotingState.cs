using System.Diagnostics.CodeAnalysis;

namespace live_trivia.Hubs;

[ExcludeFromCodeCoverage]
public class CategoryVotingState
{
    public string RoomId { get; set; } = string.Empty;


    public List<string> Categories { get; set; } = new();

    // playerId -> category
    public Dictionary<int, string> PlayerVotes { get; set; } = new();

    // 1 = first vote, 2 = revote
    public int Round { get; set; } = 1;

    // For UI timer
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int DurationSeconds { get; set; } = 60;
}

