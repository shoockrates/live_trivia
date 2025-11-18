
namespace live_trivia.Exceptions;
public class UsernameAlreadyExistsException : TriviaException
{
    public UsernameAlreadyExistsException(string username) : base($"Username '{username}' is already taken.", 409) { }
}