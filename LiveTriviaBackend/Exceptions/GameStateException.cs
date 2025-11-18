namespace live_trivia.Exceptions;
public class GameStateException : TriviaException
{
    public GameStateException(string message) : base(message, 409) { } // Conflict
}