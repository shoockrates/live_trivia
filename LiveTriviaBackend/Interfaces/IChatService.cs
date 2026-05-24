using live_trivia.DTOs.Chat;

namespace live_trivia.Interfaces;

public interface IChatService
{
    Task<ChatMessageDto> SendMessageAsync(
        string roomId,
        int playerId,
        string playerName,
        string message);

    Task<List<ChatMessageDto>> GetRoomHistoryAsync(string roomId);

   Task<bool> DeleteMessageAsync(int messageId, int playerId);

    Task<ChatMessageDto?> ToggleReactionAsync(
        int messageId,
        int playerId,
        string emoji);

    Task<ChatMessageDto> CreateSystemMessageAsync(
        string roomId,
        string text);

    Task<string?> GetMessageRoomIdAsync(int messageId);
}