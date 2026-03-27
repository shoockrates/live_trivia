using live_trivia.Dtos;
namespace live_trivia.Interfaces;
public interface IQuizService
{
    Task<List<Question>> GetQuizQuestions(string name);
    Task<Quiz?> GetQuizByName(string name);
    Task<List<Quiz>> GetAllQuizzes();
    Task<bool> SubmitQuiz(QuizDto quizDto);
}
