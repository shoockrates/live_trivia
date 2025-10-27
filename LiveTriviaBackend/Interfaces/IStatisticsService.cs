using live_trivia.Records;

namespace live_trivia.Interfaces;

public interface IStatisticsService
{
    Task UpdateGameStatisticsAsync(int playerId, string category, int score, int correctAnswers, int totalQuestions);
    Task<PlayerStatsResponse> GetPlayerStatisticsAsync(int playerId);
    Task InitializePlayerStatisticsAsync(int playerId);
}
