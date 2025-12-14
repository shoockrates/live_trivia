
namespace live_trivia.Dtos;

public record QuestionBankImportResultDto
{
    public int Total { get; init; }
    public int Added { get; init; }
    public int SkippedDuplicates { get; init; }
    public int SkippedInvalid { get; init; }
}
