namespace live_trivia.Interfaces;
public interface IQuestionService
{
    Task<List<Question>> GetAllAsync();
    Task<Question?> GetRandomAsync();
    Task<List<Question>> GetByCategoryAsync(string category);
    Task<int> LoadFromFileAsync(string filePath);
}
