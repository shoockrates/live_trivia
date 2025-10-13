namespace live_trivia
{
    public class Question : BaseEntity
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<string> Answers { get; set; } = new List<string>();
        public List<int> CorrectAnswerIndexes { get; set; } = new List<int>();
        public string Difficulty { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
        public virtual ICollection<Game> Games { get; set; } = new List<Game>();

        public Question() { }

        public Question(string text, List<string> answers, List<int> correctAnswerIndexes, string difficulty, string category)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Question text cannot be empty.", nameof(text));

            if (answers == null || answers.Count == 0)
                throw new ArgumentException("There must be at least one answer.", nameof(answers));

            if (correctAnswerIndexes == null || correctAnswerIndexes.Count == 0)
                throw new ArgumentException("There must be at least one correct answer index.", nameof(correctAnswerIndexes));

            if (correctAnswerIndexes.Any(i => i < 0 || i >= answers.Count))
                throw new ArgumentException("Correct answer index is out of range.", nameof(correctAnswerIndexes));
            Text = text;
            Answers = answers;
            CorrectAnswerIndexes = correctAnswerIndexes;
            Difficulty = difficulty;
            Category = category;
        }

        public bool IsCorrect(int answerIndex)
        {
            return CorrectAnswerIndexes.Contains(answerIndex);
        }
    }
}
