using System.ComponentModel.DataAnnotations.Schema;
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

    public Dictionary<int, string> PlayerVotes { get; set; } = new Dictionary<int, string>();
    public Dictionary<string, int> CategoryVotes { get; set; } = new Dictionary<string, int>();

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
            .Where(gp => gp.Player != null)
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
        if (CurrentQuestionIndex >= Questions.Count - 1)
        {
            // Game finished
            State = GameState.Finished;
            EndedAt = DateTime.UtcNow;
            return false;
        }

        CurrentQuestionIndex++;
        return true;
    }

    public void ScoreCurrentQuestion()
    {
        if (CurrentQuestionIndex < 0 || CurrentQuestionIndex >= Questions.Count) return;

        var question = Questions.ElementAt(CurrentQuestionIndex);
        const int TIME_LIMIT_SECONDS = 30;

        foreach (var gamePlayer in GamePlayers)
        {
            var player = gamePlayer.Player;
            var playerAnswer = PlayerAnswers
                .FirstOrDefault(pa => pa.PlayerId == player.Id && pa.QuestionId == question.Id);

            if (playerAnswer != null)
            {
                // Check if answer is correct
                playerAnswer.IsCorrect = question.CorrectAnswerIndexes.All(i => playerAnswer.SelectedAnswerIndexes.Contains(i)) &&
                                 playerAnswer.SelectedAnswerIndexes.All(i => question.CorrectAnswerIndexes.Contains(i));

                if (playerAnswer.IsCorrect)
                {
                    int totalQuestions = Questions.Count > 0 ? Questions.Count : 1;
                    double basePerQuestion = 100.0 / totalQuestions;

                    int clampedTime = Math.Max(0, Math.Min(TIME_LIMIT_SECONDS, playerAnswer.TimeLeft));
                    double timeFactor = (double)clampedTime / TIME_LIMIT_SECONDS;

                    double multiplier = 0.5 + 0.5 * timeFactor;

                    double questionScore = basePerQuestion * multiplier;

                    playerAnswer.Score = questionScore;

                    player.Score += (int)Math.Round(questionScore);

                    Console.WriteLine($"Player {player.Name} scored {questionScore:F2} points (Time left: {clampedTime}s)");
                }
                else
                {
                    playerAnswer.Score = 0;
                }
            }
        }
    }
}

