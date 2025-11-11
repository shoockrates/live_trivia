namespace live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Interfaces;
public class QuestionService : IQuestionService
{
    private readonly QuestionsRepository _questionsRepo;

    public QuestionService(QuestionsRepository questionsRepo)
    {
        _questionsRepo = questionsRepo;
    }

    public async Task<List<Question>> GetAllAsync() => await _questionsRepo.GetAllAsync();
    public async Task<Question?> GetRandomAsync() => await _questionsRepo.GetRandomAsync();
    public async Task<List<Question>> GetByCategoryAsync(string category) => await _questionsRepo.GetByCategoryAsync(category);
    public async Task<int> LoadFromFileAsync(string filePath) => await _questionsRepo.LoadFromFileAsync(filePath);
}
