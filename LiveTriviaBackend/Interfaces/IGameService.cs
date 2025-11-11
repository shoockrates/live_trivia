using live_trivia.Dtos;
namespace live_trivia.Interfaces;
public interface IGameService
{
    Task<bool> StartGameAsync(string roomId);
    Task<Game?> GetGameAsync(string roomId);
    Task<GameDetailsDto?> GetGameDetailsAsync(string roomId);
    Task<GameSettings?> GetGameSettingsAsync(string roomId);
    Task<Game> CreateGameAsync(string roomId, Player hostPlayer);
    Task<GameSettings> UpdateGameSettingsAsync(string roomId, GameSettingsDto dto);
    Task<Player?> GetPlayerByIdAsync(int playerId);
    Task AddExistingPlayerToGameAsync(Game game, Player player);
    Task SaveChangesAsync();
}
