namespace live_trivia.Dtos;

public record QuizDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public List<QuestionDto> Questions { get; set; } = new();
}
