namespace live_trivia;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int Score { get; set; }

    public int CurrentAnswerIndex { get; set; }


    public Player() { }
    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Score = 0;
        CurrentAnswerIndex = -1; // -1 means no answer selected yet
    }

    public void SubmitAnswer(int answerIndex)
    {
        CurrentAnswerIndex = answerIndex;
    }

    public void AddScore(int points)
    {
        Score += points;
    }

    public void ResetAnswer()
    {
        CurrentAnswerIndex = -1;
    }
}
