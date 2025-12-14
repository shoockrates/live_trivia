
namespace live_trivia.Dtos
{
    public record PlayerAnswerDto
    {
        public int PlayerId { get; set; }
        public int QuestionId { get; set; }
        public List<int> SelectedAnswerIndexes { get; set; } = new();
    }
}
