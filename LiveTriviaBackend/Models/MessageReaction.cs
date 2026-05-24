namespace live_trivia.Models;

public class MessageReaction : BaseEntity
{
    public int ChatMessageId { get; set; }
    public ChatMessage ChatMessage { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public string Emoji { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}