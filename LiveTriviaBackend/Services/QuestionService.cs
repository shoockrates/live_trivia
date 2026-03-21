namespace live_trivia.Services;
using live_trivia.Repositories;
using live_trivia.Dtos;
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
    public Task<List<string>> GetCategoriesAsync() => _questionsRepo.GetCategoriesAsync();

    public Task<QuestionBankImportResultDto> ImportQuestionBankAsync(QuestionBankImportDto dto)
        => _questionsRepo.ImportQuestionBankAsync(dto);
    public async Task<bool> SubmitQuestion(QuestionDto questionDto)
    {
        if (questionDto == null) throw new ArgumentNullException(nameof(questionDto));

        // Convert DTO to entity
        var question = new Question
        {
            Category = questionDto.Category,
            Difficulty = questionDto.Difficulty,
            Text = questionDto.Text,
            Answers = questionDto.Answers,
            CorrectAnswerIndexes = questionDto.CorrectAnswerIndexes
        };

        await _questionsRepo.SubmitQuestion(question);
        return true;
    }
}
