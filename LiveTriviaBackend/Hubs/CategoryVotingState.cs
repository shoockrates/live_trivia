namespace live_trivia.Hubs;

public class CategoryVotingState
{
    public string RoomId { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    // playerId -> category
    public Dictionary<int, string> PlayerVotes { get; set; } = new();
}
