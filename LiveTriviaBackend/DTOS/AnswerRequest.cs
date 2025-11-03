namespace live_trivia.Dtos
{
    public record AnswerRequest
    {
        public int QuestionId { get; set; }
        public List<int> SelectedAnswerIndexes { get; set; } = new();
    }

}