namespace live_trivia;

public class MessageReaction : BaseEntity
{
    public int Id { get; set; }
    public int ChatMessageId { get; set; }
    public ChatMessage ChatMessage { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public string Emoji { get; set; } = string.Empty;
}