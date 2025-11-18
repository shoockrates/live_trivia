using System.Collections.Concurrent;
using live_trivia.Interfaces;

namespace live_trivia.Services;

public class ActiveGamesService : IActiveGamesService
{
    private readonly ConcurrentDictionary<string, byte> _activeGames = new();

    public bool TryAddGame(string roomId)
    {
        return _activeGames.TryAdd(roomId, 0);
    }

    public bool TryRemoveGame(string roomId)
    {
        return _activeGames.TryRemove(roomId, out _);
    }

    public bool IsGameActive(string roomId)
    {
        return _activeGames.ContainsKey(roomId);
    }

    public ICollection<string> GetActiveGameIds()
    {
        return _activeGames.Keys;
    }
}

