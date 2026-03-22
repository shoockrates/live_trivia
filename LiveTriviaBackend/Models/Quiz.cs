namespace live_trivia
{
    public class Quiz : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

        public Quiz() { }
        public Quiz(string name, string difficulty, string category, List<Question> questions)
        {
            Name = name;
            Difficulty = difficulty;
            Category = category;
            Questions = questions;
        }
    }
}