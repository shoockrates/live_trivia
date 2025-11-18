namespace live_trivia.Exceptions;
public class NotEnoughQuestionsException : TriviaException
{
    public string Category { get; }
    public int RequiredCount { get; }

    public NotEnoughQuestionsException(string category, int requestedCount)
        : base($"Could not find {requestedCount} questions for category '{category}'.", 400)
    {
        Category = category;
        RequiredCount = requestedCount;
    }
}