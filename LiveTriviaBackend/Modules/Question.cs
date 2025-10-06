namespace live_trivia;
using System.Text.Json.Serialization;

public class Question
{
    public int Id { get; set; }

    [JsonPropertyName("question")]
    public string Text { get; set; }

    [JsonPropertyName("answers")]
    public List<string> Answers { get; set; }

    [JsonPropertyName("answerIndexes")]
    public List<int> CorrectAnswerIndexes { get; set; }
    public string Difficulty { get; set; }
    public string Category { get; set; }

    public Question(string text, List<string> answers, List<int> correctAnswerIndexes, string difficulty, string category)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Question text cannot be empty.", nameof(text));

        if (answers == null || answers.Count == 0)
            throw new ArgumentException("There must be at least one answer.", nameof(answers));

        if (correctAnswerIndexes == null || correctAnswerIndexes.Count == 0)
            throw new ArgumentException("There must be at least one correct answer index.", nameof(correctAnswerIndexes));

        if (correctAnswerIndexes.Any(i => i < 0 || i >= answers.Count))
            throw new ArgumentException("Correct answer index is out of range.", nameof(correctAnswerIndexes));
        Text = text;
        Answers = answers;
        CorrectAnswerIndexes = correctAnswerIndexes;
        Difficulty = difficulty;
        Category = category;
    }


    public Question()
    {
        Category = string.Empty;
        Difficulty = string.Empty;
        Text = string.Empty;
        CorrectAnswerIndexes = new List<int>();
        Answers = new List<string>();
    }

    public bool IsCorrect(int answerIndex)
    {
        return CorrectAnswerIndexes.Contains(answerIndex);
    }

}
