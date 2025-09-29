namespace live_trivia;

public class Player : IComparable<Player>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }
    public List<int> CurrentAnswerIndexes { get; set; } = new List<int>();


    public Player() { }
    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Score = 0;
        CurrentAnswerIndexes = new List<int>();
    }

    public void SubmitAnswer(int answerIndex)
    {
        if (!CurrentAnswerIndexes.Contains(answerIndex)) CurrentAnswerIndexes.Add(answerIndex);
    }

    public void ClearAnswer(int answerValue)
    {
        CurrentAnswerIndexes.Remove(answerValue);
    }

    public void ClearAnswer()
    {
        CurrentAnswerIndexes.Clear();
    }

    public void AddScore(int points)
    {
        Score += points;
    }

    public int CompareTo(Player? other)
    {
        if (other == null) return 1;
        return other.Score.CompareTo(this.Score);
    }
}
