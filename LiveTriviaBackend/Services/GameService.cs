namespace live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Dtos;

public class GameService
{
    private readonly GamesRepository _gamesRepo;

    public GameService(GamesRepository gamesRepo)
    {
        _gamesRepo = gamesRepo;
    }

    public async Task<bool> StartGameAsync(string roomId)
    {
        // Maybe some extra rules here:
        var game = await _gamesRepo.GetGameAsync(roomId);
        if (game == null)
            return false;

        if (game.State == GameState.InProgress)
            return true;

        // Could log, check some other business rules, etc.
        return await _gamesRepo.StartGameAsync(roomId);
    }

    public async Task<Game?> GetGameAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("RoomId cannot be null or empty.", nameof(roomId));

        var game = await _gamesRepo.GetGameAsync(roomId);
        return game;
    }

    public async Task<GameDetailsDto?> GetGameDetailsAsync(string roomId)
    {
        return await _gamesRepo.GetGameDetailsAsync(roomId);
    }
    public async Task<GameSettings?> GetGameSettingsAsync(string roomId)
    {
        return await _gamesRepo.GetGameSettingsAsync(roomId);
    }

    public async Task<Game> CreateGameAsync(string roomId, Player hostPlayer)
    {
        // Could enforce extra business rules like max players, etc.
        return await _gamesRepo.CreateGameAsync(roomId, hostPlayer);
    }
    public async Task<GameSettings> UpdateGameSettingsAsync(string roomId, GameSettingsDto dto)
    {
        // Business rule: maybe restrict category change if game already started
        var game = await _gamesRepo.GetGameAsync(roomId);
        if (game?.State == GameState.InProgress)
            throw new InvalidOperationException("Cannot change settings after game started.");

        return await _gamesRepo.UpdateGameSettingsAsync(roomId, dto);
    }

    public async Task<Player?> GetPlayerByIdAsync(int playerId)
    {
        return await _gamesRepo.GetPlayerByIdAsync(playerId);
    }

    public async Task AddExistingPlayerToGameAsync(Game game, Player player)
    {
        await _gamesRepo.AddExistingPlayerToGameAsync(game, player);
    }

    public async Task SaveChangesAsync()
    {
        await _gamesRepo.SaveChangesAsync();
    }
        
}
