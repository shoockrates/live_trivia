namespace live_trivia
{


    public class Game
    {

        public String roomId { get; set; }
        public int CurrentQuestionIndex { get; private set; } = -1;
        public List<Player> Players { get; set; } = new List<Player>();
        public List<Question> Questions { get; set; } = new List<Question>();

        public enum GameState { WaitingForPlayers, InProgress, Finished }
        public GameState State { get; private set; } = GameState.WaitingForPlayers;



        public Game(string roomId)
        {
            this.roomId = roomId;
        }

        public void addPlayer(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            else Players.Add(player);
        }

        public void RemovePlayer(int playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);
            if(player != null)
            {
                Players.Remove(player);
            }
        }

        public void ResetPlayersAnswers()
        {
            foreach(var player in Players)
            {
                player.ClearAnswer();
            }
        }

        public List<Player> GetLeaderboard()
        {
            return Players.OrderByDescending(p => p.Score).ToList();
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
                ResetPlayersAnswers();

                if (State == GameState.WaitingForPlayers)
                    State = GameState.InProgress;

                return true;
            }
            else
            {
                State = GameState.Finished;
                return false;
            }
        }

        public void scoreCurrentQuestion()
        {
            if (CurrentQuestionIndex < 0 || CurrentQuestionIndex >= Questions.Count) return;

            var question = Questions[CurrentQuestionIndex];
            foreach (var player in Players)
            { 
                bool isCorrect = question.CorrectAnswerIndexes.All(i => player.CurrentAnswerIndexes.Contains(i)) &&
                                 player.CurrentAnswerIndexes.All(i => question.CorrectAnswerIndexes.Contains(i));
                if (isCorrect)
                  {
                     switch (question.Difficulty)
                     {
                         case ("easy"):
                             player.Score += 1;
                             break;
                         case ("medium"):
                             player.Score += 2;
                             break;
                         case ("hard"):
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
