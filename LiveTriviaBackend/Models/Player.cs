namespace live_trivia;

public class Player : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }

    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();

    public Player()
    {
        Name = string.Empty;
    }
    public Player(int id, string name)
    {
        Id = id;
        Name = name;
        Score = 0;
    }

    // MODIFIED METHOD to include Optional and Named Arguments Requirement (4)
    public void AddScore(int points, string reason = "Game Score Update", bool logChange = false)              // Optional argument
    {
        Score += points;
        if (logChange)
        {
            Console.WriteLine($"Player {Name} (+{points} points). New Score: {Score}. Reason: {reason}");
        }
    }

    public int CompareTo(Player? other)
    {
        if (other == null) return 1;
        return other.Score.CompareTo(this.Score);
    }

    public ScoreSummary GetScoreSummary()
    {
        int correctAnswers = PlayerAnswers.Count(pa => pa.IsCorrect); // assume PlayerAnswer has IsCorrect property
        int totalScore = Score; // current total score
        return new ScoreSummary(correctAnswers, totalScore);
    }
}
