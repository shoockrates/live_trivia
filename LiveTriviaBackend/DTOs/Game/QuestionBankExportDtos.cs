namespace live_trivia.Dtos;


public record QuestionBankExportDto
{
    public int Version { get; init; } = 1;
    public DateTime ExportedAtUtc { get; init; } = DateTime.UtcNow;
    public List<QuestionExportDto> Questions { get; init; } = new();
}


public record QuestionExportDto
{
    public string Text { get; init; } = string.Empty;
    public List<string> Answers { get; init; } = new();
    public List<int> CorrectAnswerIndexes { get; init; } = new();
    public string Category { get; init; } = "Any";
    public string Difficulty { get; init; } = "medium";
}
