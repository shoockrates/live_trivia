namespace live_trivia.Exceptions;
public class GameNotFoundException : TriviaException
{
    public GameNotFoundException(string roomId) : base($"Game room '{roomId}' was not found.", 404) { }
}