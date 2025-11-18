namespace live_trivia.Exceptions;
public abstract class TriviaException : Exception
{
    public int StatusCode { get; protected set; }

    public TriviaException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }
}