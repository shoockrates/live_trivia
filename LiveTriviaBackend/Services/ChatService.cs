using live_trivia.Data;
using live_trivia.DTOs.Chat;
using live_trivia.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace live_trivia.Services;

public class ChatService : IChatService
{
    private readonly TriviaDbContext _context;

    private static readonly HashSet<string> AllowedEmojis = new()
    {
        "👍", "❤️", "😂", "😮", "😢", "🔥"
    };

    public ChatService(TriviaDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        string roomId,
        int playerId,
        string playerName,
        string message)
    {
        message = message.Trim();

        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message cannot be empty.");

        if (message.Length > 500)
            throw new InvalidOperationException("Message cannot be longer than 500 characters.");

        var game = await _context.Games
            .Include(g => g.GamePlayers)
            .FirstOrDefaultAsync(g => g.RoomId == roomId);

        if (game == null)
            throw new InvalidOperationException("Game room was not found.");

        if (game.State == GameState.InProgress)
            throw new InvalidOperationException("Chat is not available while the game is in progress.");

        if (!game.GamePlayers.Any(gp => gp.PlayerId == playerId))
            throw new InvalidOperationException("Player is not in this game room.");

        var chatMessage = new ChatMessage
        {
            GameRoomId = roomId,
            SenderPlayerId = playerId,
            MessageText = message,
            SentAt = DateTime.UtcNow,
            IsSystemMessage = false
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        chatMessage.SenderPlayer = await _context.Players.FindAsync(playerId);

        return ToDto(chatMessage);
    }

    public async Task<List<ChatMessageDto>> GetRoomHistoryAsync(string roomId)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.SenderPlayer)
            .Include(m => m.Reactions)
            .Where(m => m.GameRoomId == roomId && m.DeletedAt == null)
            .OrderByDescending(m => m.SentAt)
            .Take(100)
            .ToListAsync();

        return messages
            .OrderBy(m => m.SentAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<bool> DeleteMessageAsync(int messageId, int playerId)
    {
        var message = await _context.ChatMessages
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.DeletedAt == null);

        if (message == null)
            return false;

        if (message.IsSystemMessage)
            return false;

        if (message.SenderPlayerId != playerId)
            return false;

        message.DeletedAt = DateTime.UtcNow;

        _context.MessageReactions.RemoveRange(message.Reactions);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ChatMessageDto?> ToggleReactionAsync(
        int messageId,
        int playerId,
        string emoji)
    {
        emoji = emoji.Trim();

        if (!AllowedEmojis.Contains(emoji))
            throw new InvalidOperationException("Emoji is not allowed.");

        var message = await _context.ChatMessages
            .Include(m => m.SenderPlayer)
            .Include(m => m.Reactions)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.DeletedAt == null);

        if (message == null)
            return null;

        if (message.IsSystemMessage)
            return null;

        var game = await _context.Games
            .Include(g => g.GamePlayers)
            .FirstOrDefaultAsync(g => g.RoomId == message.GameRoomId);

        if (game == null)
            return null;

        if (!game.GamePlayers.Any(gp => gp.PlayerId == playerId))
            throw new InvalidOperationException("Player is not in this game room.");

        var existingReaction = message.Reactions
            .FirstOrDefault(r => r.PlayerId == playerId);

        if (existingReaction == null)
        {
            message.Reactions.Add(new MessageReaction
            {
                ChatMessageId = message.Id,
                PlayerId = playerId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (existingReaction.Emoji == emoji)
        {
            _context.MessageReactions.Remove(existingReaction);
        }
        else
        {
            existingReaction.Emoji = emoji;
            existingReaction.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var updatedMessage = await _context.ChatMessages
            .Include(m => m.SenderPlayer)
            .Include(m => m.Reactions)
            .FirstAsync(m => m.Id == messageId);

        return ToDto(updatedMessage);
    }

    public async Task<ChatMessageDto> CreateSystemMessageAsync(
        string roomId,
        string text)
    {
        var message = new ChatMessage
        {
            GameRoomId = roomId,
            SenderPlayerId = null,
            MessageText = text.Trim(),
            SentAt = DateTime.UtcNow,
            IsSystemMessage = true
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        return ToDto(message);
    }

    private static ChatMessageDto ToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            RoomId = message.GameRoomId,
            SenderPlayerId = message.SenderPlayerId,
            SenderName = message.IsSystemMessage
                ? "System"
                : message.SenderPlayer?.Name ?? "Unknown",
            Message = message.MessageText,
            IsSystemMessage = message.IsSystemMessage,
            SentAt = message.SentAt,
            Reactions = message.Reactions
                .GroupBy(r => r.Emoji)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<string?> GetMessageRoomIdAsync(int messageId)
    {
        return await _context.ChatMessages
            .Where(m => m.Id == messageId)
            .Select(m => m.GameRoomId)
            .FirstOrDefaultAsync();
    }
}