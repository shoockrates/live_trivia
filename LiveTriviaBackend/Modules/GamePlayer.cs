namespace live_trivia
{
    public class GamePlayer
    {
        public int Id { get; set; }
        public string GameRoomId { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public virtual Player Player { get; set; } = null!;
        public virtual Game Game { get; set; } = null!;
    }
}
