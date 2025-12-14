namespace live_trivia.Dtos;

public record GameDetailsDto
{
    public string RoomId { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int? HostPlayerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string? CurrentQuestionText { get; init; }
    public List<string>? CurrentQuestionAnswers { get; init; }
    public int CurrentQuestionIndex { get; init; }
    public int TotalQuestions { get; init; }
    public List<GamePlayerDto> Players { get; init; } = new();
    public List<QuestionDto> Questions { get; init; } = new();
    public Dictionary<string, int>? CategoryVotes { get; set; }
    public Dictionary<int, string>? PlayerVotes { get; set; }
    public List<PlayerAnswerDto> PlayerAnswers { get; set; } = new();
}
