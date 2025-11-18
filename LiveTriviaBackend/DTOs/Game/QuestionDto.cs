namespace live_trivia.Dtos;

public record QuestionDto
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<string> Answers { get; init; } = new();
    public List<int> CorrectAnswerIndexes { get; init; } = new();
    public string Category { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
}
