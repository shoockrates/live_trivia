using Xunit;
using live_trivia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace live_trivia.Tests.ModelTests
{
    public class GameModelTests
    {
        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            var game = new Game();

            Assert.Equal(string.Empty, game.RoomId);
            Assert.Equal(-1, game.CurrentQuestionIndex);
            Assert.Equal(GameState.WaitingForPlayers, game.State);
            Assert.Null(game.StartedAt);
            Assert.Null(game.EndedAt);
            Assert.Empty(game.GamePlayers);
            Assert.Empty(game.PlayerAnswers);
            Assert.Empty(game.Questions);
            Assert.Empty(game.PlayerVotes);
            Assert.Empty(game.CategoryVotes);
        }

        [Fact]
        public void Constructor_WithRoomId_SetsRoomId()
        {
            var game = new Game("test-room");

            Assert.Equal("test-room", game.RoomId);
            Assert.Equal(-1, game.CurrentQuestionIndex);
            Assert.Equal(GameState.WaitingForPlayers, game.State);
        }

        [Fact]
        public void TotalQuestions_ReturnsQuestionCount()
        {
            var game = new Game("test-room");
            game.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            game.Questions.Add(new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Easy", "History"));

            Assert.Equal(2, game.TotalQuestions);
        }

        [Fact]
        public void AddPlayer_ThrowsException_WhenPlayerIsNull()
        {
            var game = new Game("test-room");

            Assert.Throws<ArgumentNullException>(() => game.AddPlayer(null!));
        }

        [Fact]
        public void AddPlayer_AddsPlayerToGame()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "TestPlayer" };

            game.AddPlayer(player);

            Assert.Single(game.GamePlayers);
            Assert.Equal(1, game.GamePlayers.First().PlayerId);
            Assert.Equal("test-room", game.GamePlayers.First().GameRoomId);
        }

        [Fact]
        public void RemovePlayer_RemovesPlayer_WhenExists()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "TestPlayer" };
            game.AddPlayer(player);

            game.RemovePlayer(1);

            Assert.Empty(game.GamePlayers);
        }

        [Fact]
        public void RemovePlayer_DoesNothing_WhenPlayerNotFound()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "TestPlayer" };
            game.AddPlayer(player);

            game.RemovePlayer(999);

            Assert.Single(game.GamePlayers);
        }

        [Fact]
        public void GetLeaderboard_ReturnsEmptyList_WhenNoPlayers()
        {
            var game = new Game("test-room");

            var leaderboard = game.GetLeaderboard();

            Assert.Empty(leaderboard);
        }

        [Fact]
        public void GetLeaderboard_ReturnsPlayers_OrderedByScore()
        {
            var game = new Game("test-room");
            var player1 = new Player { Id = 1, Name = "Player1", Score = 100 };
            var player2 = new Player { Id = 2, Name = "Player2", Score = 200 };
            var player3 = new Player { Id = 3, Name = "Player3", Score = 150 };
            game.AddPlayer(player1);
            game.AddPlayer(player2);
            game.AddPlayer(player3);

            var leaderboard = game.GetLeaderboard();

            Assert.Equal(3, leaderboard.Count);
            Assert.Equal(2, leaderboard[0].Id); // Highest score
            Assert.Equal(3, leaderboard[1].Id);
            Assert.Equal(1, leaderboard[2].Id);
        }

        [Fact]
        public void SetQuestions_SetsQuestions_AndResetsIndex()
        {
            var game = new Game("test-room");
            game.CurrentQuestionIndex = 5;
            var questions = new List<Question>
            {
                new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"),
                new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Easy", "History")
            };

            game.SetQuestions(questions);

            Assert.Equal(2, game.Questions.Count);
            Assert.Equal(-1, game.CurrentQuestionIndex);
        }

        [Fact]
        public void MoveNextQuestion_ReturnsTrue_WhenMoreQuestions()
        {
            var game = new Game("test-room");
            game.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            game.Questions.Add(new Question("Q2", new List<string> { "A" }, new List<int> { 0 }, "Easy", "History"));
            game.CurrentQuestionIndex = -1;

            var result = game.MoveNextQuestion();

            Assert.True(result);
            Assert.Equal(0, game.CurrentQuestionIndex);
        }

        [Fact]
        public void MoveNextQuestion_ReturnsFalse_WhenLastQuestion()
        {
            var game = new Game("test-room");
            game.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            game.CurrentQuestionIndex = 0;

            var result = game.MoveNextQuestion();

            Assert.False(result);
            Assert.Equal(GameState.Finished, game.State);
            Assert.NotNull(game.EndedAt);
        }

        [Fact]
        public void MoveNextQuestion_SetsGameToFinished_WhenNoMoreQuestions()
        {
            var game = new Game("test-room");
            game.Questions.Add(new Question("Q1", new List<string> { "A" }, new List<int> { 0 }, "Easy", "Geography"));
            game.CurrentQuestionIndex = 0;
            game.State = GameState.InProgress;

            game.MoveNextQuestion();

            Assert.Equal(GameState.Finished, game.State);
            Assert.NotNull(game.EndedAt);
        }

        [Fact]
        public void ScoreCurrentQuestion_DoesNothing_WhenIndexOutOfRange()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "Player1", Score = 0 };
            game.AddPlayer(player);
            game.CurrentQuestionIndex = -1;

            game.ScoreCurrentQuestion();

            Assert.Equal(0, player.Score);
        }

        [Fact]
        public void ScoreCurrentQuestion_DoesNothing_WhenNoAnswer()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "Player1", Score = 0 };
            game.AddPlayer(player);
            var question = new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            game.Questions.Add(question);
            game.CurrentQuestionIndex = 0;

            game.ScoreCurrentQuestion();

            Assert.Equal(0, player.Score);
        }

        [Fact]
        public void ScoreCurrentQuestion_AwardsPoints_WhenAnswerCorrect()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "Player1", Score = 0 };
            game.AddPlayer(player);
            var question = new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            question.Id = 1;
            game.Questions.Add(question);
            game.CurrentQuestionIndex = 0;

            var answer = new PlayerAnswer
            {
                PlayerId = 1,
                QuestionId = 1,
                SelectedAnswerIndexes = new List<int> { 0 },
                TimeLeft = 20
            };
            game.PlayerAnswers.Add(answer);

            game.ScoreCurrentQuestion();

            Assert.True(answer.IsCorrect);
            Assert.True(player.Score > 0);
        }

        [Fact]
        public void ScoreCurrentQuestion_NoPoints_WhenAnswerIncorrect()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "Player1", Score = 0 };
            game.AddPlayer(player);
            var question = new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            question.Id = 1;
            game.Questions.Add(question);
            game.CurrentQuestionIndex = 0;

            var answer = new PlayerAnswer
            {
                PlayerId = 1,
                QuestionId = 1,
                SelectedAnswerIndexes = new List<int> { 1 }, // Wrong answer
                TimeLeft = 20
            };
            game.PlayerAnswers.Add(answer);

            game.ScoreCurrentQuestion();

            Assert.False(answer.IsCorrect);
            Assert.Equal(0, answer.Score);
            Assert.Equal(0, player.Score);
        }

        [Fact]
        public void ScoreCurrentQuestion_CalculatesScore_WithTimeFactor()
        {
            var game = new Game("test-room");
            var player = new Player { Id = 1, Name = "Player1", Score = 0 };
            game.AddPlayer(player);
            var question = new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            question.Id = 1;
            game.Questions.Add(question);
            game.CurrentQuestionIndex = 0;

            var answer = new PlayerAnswer
            {
                PlayerId = 1,
                QuestionId = 1,
                SelectedAnswerIndexes = new List<int> { 0 },
                TimeLeft = 30 // Maximum time
            };
            game.PlayerAnswers.Add(answer);

            game.ScoreCurrentQuestion();

            Assert.True(answer.Score > 0);
            Assert.True(player.Score > 0);
        }

        [Fact]
        public void IsVotingActive_ReturnsFalse_WhenNoVotes()
        {
            var game = new Game("test-room");

            Assert.False(game.IsVotingActive());
        }

        [Fact]
        public void IsVotingActive_ReturnsTrue_WhenVotesExist()
        {
            var game = new Game("test-room");
            game.CategoryVotes["Geography"] = 1;

            Assert.True(game.IsVotingActive());
        }

        [Fact]
        public void StartVoting_ThrowsException_WhenCategoriesNull()
        {
            var game = new Game("test-room");

            Assert.Throws<ArgumentException>(() => game.StartVoting(null!));
        }

        [Fact]
        public void StartVoting_ThrowsException_WhenCategoriesEmpty()
        {
            var game = new Game("test-room");

            Assert.Throws<ArgumentException>(() => game.StartVoting(new List<string>()));
        }

        [Fact]
        public void StartVoting_InitializesVoting()
        {
            var game = new Game("test-room");
            var categories = new List<string> { "Geography", "History", "Science" };

            game.StartVoting(categories);

            Assert.Equal(3, game.CategoryVotes.Count);
            Assert.True(game.CategoryVotes.ContainsKey("Geography"));
            Assert.True(game.CategoryVotes.ContainsKey("History"));
            Assert.True(game.CategoryVotes.ContainsKey("Science"));
            Assert.Equal(0, game.CategoryVotes["Geography"]);
            Assert.Empty(game.PlayerVotes);
        }

        [Fact]
        public void StartVoting_RemovesDuplicates()
        {
            var game = new Game("test-room");
            var categories = new List<string> { "Geography", "Geography", "History" };

            game.StartVoting(categories);

            Assert.Equal(2, game.CategoryVotes.Count);
        }

        [Fact]
        public void ClearVoting_ClearsAllVotes()
        {
            var game = new Game("test-room");
            game.CategoryVotes["Geography"] = 5;
            game.PlayerVotes[1] = "Geography";

            game.ClearVoting();

            Assert.Empty(game.CategoryVotes);
            Assert.Empty(game.PlayerVotes);
        }

        [Fact]
        public void GetWinningCategory_ReturnsNull_WhenNoVotes()
        {
            var game = new Game("test-room");

            var winner = game.GetWinningCategory();

            Assert.Null(winner);
        }

        [Fact]
        public void GetWinningCategory_ReturnsNull_WhenNoPlayerVotes()
        {
            var game = new Game("test-room");
            game.CategoryVotes["Geography"] = 0;

            var winner = game.GetWinningCategory();

            Assert.Null(winner);
        }

        [Fact]
        public void GetWinningCategory_ReturnsWinner_WhenClearWinner()
        {
            var game = new Game("test-room");
            game.CategoryVotes["Geography"] = 5;
            game.CategoryVotes["History"] = 2;
            game.PlayerVotes[1] = "Geography";
            game.PlayerVotes[2] = "Geography";

            var winner = game.GetWinningCategory();

            Assert.Equal("Geography", winner);
        }

        [Fact]
        public void GetWinningCategory_ReturnsFirstAlphabetically_WhenTie()
        {
            var game = new Game("test-room");
            game.CategoryVotes["History"] = 3;
            game.CategoryVotes["Geography"] = 3;
            game.PlayerVotes[1] = "Geography";
            game.PlayerVotes[2] = "History";

            var winner = game.GetWinningCategory();

            Assert.Equal("Geography", winner); // Alphabetically first
        }

        [Fact]
        public void PlayerVotes_Getter_ReturnsEmptyDictionary_WhenNull()
        {
            var game = new Game("test-room");
            // Access the property to ensure it doesn't return null
            var votes = game.PlayerVotes;

            Assert.NotNull(votes);
            Assert.Empty(votes);
        }

        [Fact]
        public void CategoryVotes_Getter_ReturnsEmptyDictionary_WhenNull()
        {
            var game = new Game("test-room");
            var votes = game.CategoryVotes;

            Assert.NotNull(votes);
            Assert.Empty(votes);
        }
    }
}
