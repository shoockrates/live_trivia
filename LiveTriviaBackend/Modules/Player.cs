namespace live_trivia;

public class Player : BaseEntity, IComparable<Player>
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
