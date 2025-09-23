namespace live_trivia;

public class Player
{
    public string Name { get; set; }
    public int Score { get; set; }
    public int CurrentAnswerIndex { get; set; }
    public int Id { get; set; }

    public Player(string name, int score, int currentAnswerIndex, int id)
    {
        Name = name;
        Score = score;
        CurrentAnswerIndex = currentAnswerIndex;
        Id = id;
    }

    // Constructor for creating a new player
    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Score = 0; // Initialize score to zero
        CurrentAnswerIndex = -1; // -1 = no answer selected yet
    }

    public void SubmitAnswer(int answerIndex)
    {
        CurrentAnswerIndex = answerIndex;
    }

    public void AddScore(int points)
    {
        Score += points;
    }
}
