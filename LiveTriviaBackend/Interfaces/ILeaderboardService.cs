using live_trivia.Records;

namespace live_trivia.Interfaces;
public interface ILeaderboardService
{
    Task<List<LeaderboardEntry>> GetTopPlayersAsync(int topCount = 10);
    Task<List<LeaderboardEntry>> GetTopPlayersByCategoryAsync(string category, int topCount = 10);
    Task<List<string>> GetAvailableCategoriesAsync();
}
