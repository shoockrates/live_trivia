using live_trivia.Dtos;
namespace live_trivia.Interfaces;
public interface IQuestionService
{
    Task<List<Question>> GetAllAsync();
    Task<Question?> GetRandomAsync();
    Task<List<Question>> GetByCategoryAsync(string category);
    Task<int> LoadFromFileAsync(string filePath);
    Task<List<string>> GetCategoriesAsync();
    Task<QuestionBankImportResultDto> ImportQuestionBankAsync(QuestionBankImportDto dto);
}
