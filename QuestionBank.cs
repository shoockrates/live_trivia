using System.Text.Json;
namespace live_trivia;
public class QuestionBank
{
    // List of all questions 
    public List<Question> Questions { get; private set; } = new List<Question>();

    // Constructor loads questions from a JSON file 
    public QuestionBank(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
        var json = File.ReadAllText(jsonFilePath);
        Questions = JsonSerializer.Deserialize<List<Question>>(json) ?? new List<Question>();
    }
}
