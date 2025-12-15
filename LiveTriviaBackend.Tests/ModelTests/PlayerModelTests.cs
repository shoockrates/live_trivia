using System.Collections.Generic;
using live_trivia;
using Xunit;

namespace live_trivia.Tests.ModelTests
{
    public class PlayerModelTests
    {
        [Fact]
        public void Constructor_SetsDefaults()
        {
            var player = new Player();

            Assert.Equal(0, player.Id);
            Assert.Equal(string.Empty, player.Name);
            Assert.Equal(0, player.Score);
            Assert.NotNull(player.GamePlayers);
            Assert.NotNull(player.PlayerAnswers);
        }

        [Fact]
        public void Constructor_WithParameters_InitializesProperties()
        {
            var player = new Player(5, "Alice");

            Assert.Equal(5, player.Id);
            Assert.Equal("Alice", player.Name);
            Assert.Equal(0, player.Score);
        }

        [Fact]
        public void AddScore_IncreasesScore_WithoutLogging()
        {
            var player = new Player(1, "Bob");

            player.AddScore(10);

            Assert.Equal(10, player.Score);
        }

        [Fact]
        public void CompareTo_OrdersByScoreDescending()
        {
            var high = new Player(1, "High") { Score = 20 };
            var low = new Player(2, "Low") { Score = 5 };

            Assert.True(high.CompareTo(low) < 0);
            Assert.True(low.CompareTo(high) > 0);
            Assert.Equal(0, high.CompareTo(new Player(3, "Other") { Score = 20 }));
        }

        [Fact]
        public void GetScoreSummary_ComputesFromAnswersAndScore()
        {
            var player = new Player(1, "Bob") { Score = 15 };
            player.PlayerAnswers = new List<PlayerAnswer>
            {
                new PlayerAnswer { IsCorrect = true },
                new PlayerAnswer { IsCorrect = false },
                new PlayerAnswer { IsCorrect = true }
            };

            var summary = player.GetScoreSummary();

            Assert.Equal(2, summary.CorrectAnswers);
            Assert.Equal(15, summary.TotalScore);
        }
    }
}


