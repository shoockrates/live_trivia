namespace live_trivia
{
    public class PlayerAnswer
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int QuestionId { get; set; }
        public string GameRoomId { get; set; } = string.Empty;
        public List<int> SelectedAnswerIndexes { get; set; } = new List<int>();
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Player Player { get; set; } = null!;
        public virtual Question Question { get; set; } = null!;
        public virtual Game Game { get; set; } = null!;
    }
}
