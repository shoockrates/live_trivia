namespace live_trivia.Interfaces;

public interface IActiveGamesService
{
    bool TryAddGame(string roomId);
    bool TryRemoveGame(string roomId);
    bool IsGameActive(string roomId);
    ICollection<string> GetActiveGameIds();
}

