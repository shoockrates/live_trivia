namespace live_trivia.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }

    public string RoomId { get; set; } = string.Empty;

    public int? SenderPlayerId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsSystemMessage { get; set; }

    public DateTime SentAt { get; set; }

    public Dictionary<string, int> Reactions { get; set; }
        = new();
}