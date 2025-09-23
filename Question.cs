namespace live_trivia;

public class Question
{
    public string Text { get; set; }
    public List<string> Answers { get; set; }
    public List<int> CorrectAnswerIndexes { get; set; }
    public string Difficulty { get; set; }
    public string Category { get; set; }

    public Question(string text, List<string> answers, List<int> correctAnswerIndexes, string difficulty, string category)
    {
        Text = text;
        Answers = answers;
        CorrectAnswerIndexes = correctAnswerIndexes;
        Difficulty = difficulty;
        Category = category;
    }

}


