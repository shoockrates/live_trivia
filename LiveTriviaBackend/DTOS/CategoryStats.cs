namespace LiveTrivia.Dtos;

public record CategoryStat
{
    public string Category { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double Accuracy { get; set; }
}
