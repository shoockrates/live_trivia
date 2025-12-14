
namespace live_trivia.Dtos;


public record QuestionBankImportDto
{
    public int Version { get; init; } = 1;
    public List<QuestionImportDto> Questions { get; init; } = new();
}

public record QuestionImportDto
{
    public string Text { get; init; } = string.Empty;
    public List<string> Answers { get; init; } = new();
    public List<int> CorrectAnswerIndexes { get; init; } = new();
    public string Category { get; init; } = "Any";
    public string Difficulty { get; init; } = "medium";
}
