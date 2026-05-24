namespace live_trivia.DTOs.Chat;

public class SendChatMessageDto
{
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}