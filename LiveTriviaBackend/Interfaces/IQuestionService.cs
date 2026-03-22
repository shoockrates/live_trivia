using live_trivia.Dtos;
namespace live_trivia.Interfaces;
public interface IQuestionService
{
    Task<List<Question>> GetAllAsync();
    Task<Question?> GetRandomAsync();
    Task<List<Question>> GetByCategoryAsync(string category);
    Task<int> LoadFromFileAsync(string filePath);
    Task<List<string>> GetCategoriesAsync();
    Task<List<Question>> GetQuizQuestions(string name);
    Task<Quiz?> GetQuizByName(string name);
    Task<List<Quiz>> GetAllQuizzes();
    Task<QuestionBankImportResultDto> ImportQuestionBankAsync(QuestionBankImportDto dto);
    Task<bool> SubmitQuestion(QuestionDto questionDto);
    Task<bool> SubmitQuiz(QuizDto quizDto);
}
