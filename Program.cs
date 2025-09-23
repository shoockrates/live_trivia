namespace live_trivia;

class Program
{
    public List<Player> Players { get; set; } = new List<Player>();
    public QuestionBank QuestionBank { get; set; }
    public Question CurrentQuestion { get; set; }
    public int CurrentRound { get; set; }

}

