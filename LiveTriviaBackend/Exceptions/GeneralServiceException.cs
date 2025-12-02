namespace live_trivia.Exceptions;
public class GeneralServiceException : TriviaException 
{
    public GeneralServiceException(string message, int statusCode = 500) 
        : base(message, statusCode) { }
}