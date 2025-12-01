namespace live_trivia.Exceptions;
public class PlayerNotFoundException : TriviaException
{
    public PlayerNotFoundException(int playerId)
        : base($"Player with ID {playerId} was not found.")
    {
    }
}