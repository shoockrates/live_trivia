namespace live_trivia
{
    public struct ScoreSummary
    {
        public int CorrectAnswers { get; set; }
        public int TotalScore { get; set; }

        public ScoreSummary(int correctAnswers, int totalScore)
        {
            CorrectAnswers = correctAnswers;
            TotalScore = totalScore;
        }
        public override string ToString() => $"Correct: {CorrectAnswers}, Score: {TotalScore}";
    }
} 