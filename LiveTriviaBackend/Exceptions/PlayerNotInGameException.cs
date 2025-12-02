namespace live_trivia.Exceptions;
public class PlayerNotInGameException : TriviaException
{
    public PlayerNotInGameException(int playerId, string roomId)
        : base($"Player with ID {playerId} is not in the game with Room ID {roomId}.", 400)
    {
    }
}