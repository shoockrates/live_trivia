namespace live_trivia;

public class Game : BaseEntity
{
    public string RoomId { get; set; } = string.Empty;
    public int CurrentQuestionIndex { get; set; } = -1;
    public GameState State { get; set; } = GameState.WaitingForPlayers;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalQuestions => Questions.Count;

    public int? HostPlayerId { get; set; }          // Foreign key
    public Player? HostPlayer { get; set; }         // Navigation property
    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public Game() { }

    public Game(string roomId)
    {
        this.RoomId = roomId;
    }

    public void AddPlayer(Player player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var gamePlayer = new GamePlayer
        {
            GameRoomId = RoomId,
            PlayerId = player.Id,
            Player = player
        };
        GamePlayers.Add(gamePlayer);
    }

    public void RemovePlayer(int playerId)
    {
        var gamePlayer = GamePlayers.FirstOrDefault(gp => gp.PlayerId == playerId);
        if (gamePlayer != null)
        {
            GamePlayers.Remove(gamePlayer);
        }
    }

    public List<Player> GetLeaderboard()
    {
        return GamePlayers
            .Select(gp => gp.Player)
            .OrderByDescending(p => p.Score)
            .ToList();
    }

    public void SetQuestions(List<Question> questions)
    {
        Questions = questions;
        CurrentQuestionIndex = -1;
    }

    public bool MoveNextQuestion()
    {
        if (CurrentQuestionIndex + 1 < Questions.Count)
        {
            CurrentQuestionIndex++;
            if (State == GameState.WaitingForPlayers)
            {
                State = GameState.InProgress;
                StartedAt = DateTime.UtcNow;
            }

            return true;
        }
        else
        {
            State = GameState.Finished;
            EndedAt = DateTime.UtcNow;
            return false;
        }
    }

    public void ScoreCurrentQuestion()
    {
        if (CurrentQuestionIndex < 0 || CurrentQuestionIndex >= Questions.Count) return;

        var question = Questions.ElementAt(CurrentQuestionIndex);
        foreach (var gamePlayer in GamePlayers)
        {
            var player = gamePlayer.Player;
            var playerAnswer = PlayerAnswers
                .FirstOrDefault(pa => pa.PlayerId == player.Id && pa.QuestionId == question.Id);

            if (playerAnswer != null)
            {
                playerAnswer.IsCorrect = question.CorrectAnswerIndexes.All(i => playerAnswer.SelectedAnswerIndexes.Contains(i)) &&
                                 playerAnswer.SelectedAnswerIndexes.All(i => question.CorrectAnswerIndexes.Contains(i));
                if (playerAnswer.IsCorrect)
                {
                    switch (question.Difficulty.ToLower())
                    {
                        case "easy":
                            player.Score += 1;
                            break;
                        case "medium":
                            player.Score += 2;
                            break;
                        case "hard":
                            player.Score += 3;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}

