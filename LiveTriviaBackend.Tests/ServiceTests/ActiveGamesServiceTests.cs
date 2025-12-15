using System.Collections.Generic;
using live_trivia.Services;
using Xunit;

namespace live_trivia.Tests.ServiceTests
{
    public class ActiveGamesServiceTests
    {
        [Fact]
        public void TryAddGame_ReturnsTrue_WhenNewRoomId()
        {
            var service = new ActiveGamesService();

            var result = service.TryAddGame("room1");

            Assert.True(result);
            Assert.True(service.IsGameActive("room1"));
        }

        [Fact]
        public void TryAddGame_ReturnsFalse_WhenDuplicate()
        {
            var service = new ActiveGamesService();

            Assert.True(service.TryAddGame("room1"));
            var result = service.TryAddGame("room1");

            Assert.False(result);
        }

        [Fact]
        public void TryRemoveGame_RemovesExistingGame()
        {
            var service = new ActiveGamesService();
            service.TryAddGame("room1");

            var removed = service.TryRemoveGame("room1");

            Assert.True(removed);
            Assert.False(service.IsGameActive("room1"));
        }

        [Fact]
        public void TryRemoveGame_ReturnsFalse_WhenNotPresent()
        {
            var service = new ActiveGamesService();

            var removed = service.TryRemoveGame("unknown");

            Assert.False(removed);
        }

        [Fact]
        public void GetActiveGameIds_ReturnsAllActiveRooms()
        {
            var service = new ActiveGamesService();
            service.TryAddGame("room1");
            service.TryAddGame("room2");

            var ids = service.GetActiveGameIds();

            Assert.Contains("room1", ids);
            Assert.Contains("room2", ids);
            Assert.Equal(2, new List<string>(ids).Count);
        }
    }
}


