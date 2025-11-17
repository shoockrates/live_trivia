namespace live_trivia.Exceptions;

public class NotEnoughQuestionsException : Exception
{
    public string Category { get; }
    public int RequiredCount { get; }

    public NotEnoughQuestionsException(string category, int required)
        : base($"Not enough questions available in category '{category}'. Required: {required}.")
    {
        Category = category;
        RequiredCount = required;
    }
}
