namespace live_trivia.Models;

public class ChatMessage : BaseEntity
{
    public string GameRoomId { get; set; } = string.Empty;

    public int? SenderPlayerId { get; set; }
    public Player? SenderPlayer { get; set; }

    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsSystemMessage { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<MessageReaction> Reactions { get; set; }
        = new List<MessageReaction>();
}