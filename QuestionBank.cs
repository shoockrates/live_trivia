using System.Text.Json;
namespace live_trivia;
public class QuestionBank
{
    public List<Question> Questions { get; private set; } = new List<Question>();
    private Random _random = new Random();
    public int Count => Questions.Count;

    public QuestionBank() { }

    public QuestionBank(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");

        var json = File.ReadAllText(jsonFilePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Questions = JsonSerializer.Deserialize<List<Question>>(json, options) ?? new List<Question>();
    }

    // Returns a random question from the list
    public Question GetRandomQuestion()
    {
        if (Questions.Count == 0)
            throw new InvalidOperationException("No questions available.");

        int index = _random.Next(Questions.Count);
        return Questions[index];
    }


    public Question GetQuestionByCategory(string category)
    {
        var filtered = Questions.Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (filtered.Count == 0)
            throw new InvalidOperationException($"No questions found for category '{category}'.");
        int index = _random.Next(filtered.Count);
        return filtered[index];
    }


    public Question GetQuestionByDifficulty(string difficulty)
    {
        var filtered = Questions.Where(q => q.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase)).ToList();
        if (filtered.Count == 0)
            throw new InvalidOperationException($"No questions found for difficulty '{difficulty}'.");

        int index = _random.Next(filtered.Count);
        return filtered[index];
    }


    public Question GetQuestion(string category, string difficulty)
    {
        var filtered = Questions
            .Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                     && q.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtered.Count == 0)
            throw new InvalidOperationException($"No questions found for category '{category}' and difficulty '{difficulty}'.");

        int index = _random.Next(filtered.Count);
        return filtered[index];
    }


    public List<string> GetAllCategories()
    {
        return Questions.Select(q => q.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }


    public List<string> GetAllDifficulties()
    {
        return Questions.Select(q => q.Difficulty).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }


    public void RemoveQuestion(Question question)
    {
        Questions.Remove(question);
    }

}
