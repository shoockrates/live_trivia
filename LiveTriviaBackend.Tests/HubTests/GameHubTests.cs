using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using live_trivia;
using live_trivia.Data;
using live_trivia.Dtos;
using live_trivia.Hubs;
using live_trivia.Interfaces;
using live_trivia.Repositories;

namespace live_trivia.Tests.HubTests
{
    public class GameHubTests
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly Mock<IActiveGamesService> _mockActiveGamesService;

        public GameHubTests()
        {
            _mockGameService = new Mock<IGameService>();
            _mockActiveGamesService = new Mock<IActiveGamesService>();

            // Clear the static _categoryVotingStates between tests without reassigning the readonly field
            var field = typeof(GameHub).GetField("_categoryVotingStates",
                BindingFlags.Static | BindingFlags.NonPublic);
            var dictObj = field?.GetValue(null);
            if (dictObj is System.Collections.IDictionary dict)
            {
                dict.Clear();
            }
        }

        private TriviaDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<TriviaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TriviaDbContext(options);
        }

        private (GameHub hub,
                 Mock<HubCallerContext> ctx,
                 Mock<IHubCallerClients> clients,
                 Mock<ISingleClientProxy> caller,
                 Mock<IClientProxy> group,
                 Mock<IGroupManager> groups)
            CreateHub(ClaimsPrincipal? user = null)
        {
            var db = CreateInMemoryDb();
            var gamesRepo = new GamesRepository(db);

            var ctx = new Mock<HubCallerContext>();
            ctx.SetupGet(c => c.ConnectionId).Returns("conn-123");
            if (user != null)
                ctx.SetupGet(c => c.User).Returns(user);

            var caller = new Mock<ISingleClientProxy>();
            var group = new Mock<IClientProxy>();
            var clients = new Mock<IHubCallerClients>();
            clients.SetupGet(c => c.Caller).Returns(caller.Object);
            clients.Setup(c => c.Group(It.IsAny<string>()))
                   .Returns(group.Object);

            var groups = new Mock<IGroupManager>();

            var hub = new GameHub(_mockGameService.Object, gamesRepo, _mockActiveGamesService.Object)
            {
                Context = ctx.Object,
                Clients = clients.Object,
                Groups = groups.Object
            };

            return (hub, ctx, clients, caller, group, groups);
        }

        private ClaimsPrincipal CreateUser(int? playerId = null, string name = "TestPlayer")
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, name) };

            if (playerId.HasValue)
                claims.Add(new Claim("playerId", playerId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }

        // --------------------------------------------------
        // JoinGameRoom
        // --------------------------------------------------

        [Fact]
        public async Task JoinGameRoom_SendsError_WhenPlayerIdMissing()
        {
            var user = CreateUser(playerId: null);
            var (hub, _, _, caller, _, _) = CreateHub(user);

            await hub.JoinGameRoom("room1");

            caller.Verify(
                c => c.SendCoreAsync(
                    "Error",
                    It.Is<object[]>(args => (string)args[0] == "Player identity not found"),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task JoinGameRoom_SendsError_WhenGameNotFound()
        {
            var user = CreateUser(1);
            var (hub, _, _, caller, groupProxy, groups) = CreateHub(user);

            _mockGameService.Setup(s => s.GetGameDetailsAsync("room1"))
                .ReturnsAsync((GameDetailsDto?)null);

            await hub.JoinGameRoom("room1");

            groups.Verify(g => g.AddToGroupAsync("conn-123", "room1", default), Times.Once);

            caller.Verify(
                c => c.SendCoreAsync(
                    "Error",
                    It.Is<object[]>(args => (string)args[0] == "Game not found"),
                    default),
                Times.Once);

            groupProxy.Verify(
                g => g.SendCoreAsync("PlayerJoined", It.IsAny<object[]>(), default),
                Times.Never);
        }

        [Fact]
        public async Task JoinGameRoom_BroadcastsPlayerJoined_WhenGameExists()
        {
            var user = CreateUser(5, "Alice");
            var (hub, _, _, _, groupProxy, groups) = CreateHub(user);

            var details = new GameDetailsDto { RoomId = "room1" };
            _mockGameService.Setup(s => s.GetGameDetailsAsync("room1"))
                .ReturnsAsync(details);

            await hub.JoinGameRoom("room1");

            groups.Verify(g => g.AddToGroupAsync("conn-123", "room1", default), Times.Once);
            groupProxy.Verify(
                g => g.SendCoreAsync("PlayerJoined", It.IsAny<object[]>(), default),
                Times.Once);
        }

        // --------------------------------------------------
        // StartGame
        // --------------------------------------------------

        [Fact]
        public async Task StartGame_SendsError_WhenPlayerIdMissing()
        {
            var user = CreateUser(playerId: null);
            var (hub, _, _, caller, _, _) = CreateHub(user);

            await hub.StartGame("room1");

            caller.Verify(
                c => c.SendCoreAsync(
                    "GameStartFailed",
                    It.Is<object[]>(args => (string)args[0] == "Player identity not found"),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task StartGame_SendsError_WhenGameNotFound()
        {
            var user = CreateUser(1);
            var (hub, _, _, caller, _, _) = CreateHub(user);

            _mockGameService.Setup(s => s.GetGameAsync("room1"))
                .ReturnsAsync((Game?)null);

            await hub.StartGame("room1");

            caller.Verify(
                c => c.SendCoreAsync(
                    "GameStartFailed",
                    It.Is<object[]>(args => (string)args[0] == "Game not found"),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task StartGame_SendsError_WhenNotHost()
        {
            var user = CreateUser(2);
            var (hub, _, _, caller, _, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            game.GamePlayers.Add(new GamePlayer { Player = new Player { Id = 2 } });

            _mockGameService.Setup(s => s.GetGameAsync("room1"))
                .ReturnsAsync(game);

            await hub.StartGame("room1");

            caller.Verify(
                c => c.SendCoreAsync(
                    "GameStartFailed",
                    It.Is<object[]>(args => (string)args[0] == "Only the host can start the game"),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task StartGame_BroadcastsGameStarted_WhenServiceReturnsSuccess()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, groupProxy, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            game.GamePlayers.Add(new GamePlayer { Player = new Player { Id = 1 } });

            var details = new GameDetailsDto { RoomId = "room1" };

            _mockGameService.Setup(s => s.GetGameAsync("room1")).ReturnsAsync(game);
            _mockGameService.Setup(s => s.StartGameAsync("room1")).ReturnsAsync(true);
            _mockGameService.Setup(s => s.GetGameDetailsAsync("room1")).ReturnsAsync(details);

            await hub.StartGame("room1");

            groupProxy.Verify(
                g => g.SendCoreAsync("GameStarted", It.IsAny<object[]>(), default),
                Times.Once);
        }

        // --------------------------------------------------
        // SubmitCategoryVote
        // --------------------------------------------------

        [Fact]
        public async Task SubmitCategoryVote_UpdatesTallies_AndBroadcasts()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, groupProxy, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync("room1")).ReturnsAsync(game);

            await hub.StartCategoryVoting("room1", new List<string> { "Geo", "History" });

            await hub.SubmitCategoryVote("room1", "Geo");

            groupProxy.Verify(
                g => g.SendCoreAsync("CategoryVoteUpdated", It.IsAny<object[]>(), default),
                Times.Once);
        }

        // --------------------------------------------------
        // OnConnected / OnDisconnected
        // --------------------------------------------------

        [Fact]
        public async Task OnConnectedAsync_CompletesWithoutError()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, _, _) = CreateHub(user);

            await hub.OnConnectedAsync();
        }

        [Fact]
        public async Task OnDisconnectedAsync_CompletesWithoutError()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, _, _) = CreateHub(user);

            await hub.OnDisconnectedAsync(new Exception("test"));
        }

        // --------------------------------------------------
        // Category Voting: EndCategoryVoting
        // --------------------------------------------------

        [Fact]
        public async Task EndCategoryVoting_NoVotes_BroadcastsFinishedWithNullWinner()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, groupProxy, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync("room1")).ReturnsAsync(game);

            await hub.StartCategoryVoting("room1", new List<string> { "Geo", "History" });
            await hub.EndCategoryVoting("room1");

            groupProxy.Verify(
                g => g.SendCoreAsync(
                    "CategoryVotingFinished",
                    It.Is<object[]>(args => CategoryVotingFinishedNoWinnerMatches(args)),
                    default),
                Times.Once);

        }

        [Fact]
        public async Task EndCategoryVoting_ClearWinner_UpdatesGameSettingsAndBroadcasts()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, groupProxy, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync("room1")).ReturnsAsync(game);

            _mockGameService
                .Setup(s => s.GetGameSettingsAsync("room1"))
                .ReturnsAsync(new GameSettings
                {
                    Difficulty = "medium",
                    QuestionCount = 10,
                    TimeLimitSeconds = 40
                });


            await hub.StartCategoryVoting("room1", new List<string> { "Geo", "History" });

            dynamic state = GetVotingState("room1");
            state.PlayerVotes[1] = "Geo";

            await hub.EndCategoryVoting("room1");

            _mockGameService.Verify(
                s => s.UpdateGameSettingsAsync(
                    "room1",
                    It.Is<GameSettingsDto>(dto =>
                        dto.Category == "Geo" &&
                        dto.Difficulty == "medium" &&
                        dto.QuestionCount == 10 &&
                        dto.TimeLimitSeconds == 40)),
                Times.Once);

            groupProxy.Verify(
                g => g.SendCoreAsync(
                    "CategoryVotingFinished",
                    It.Is<object[]>(args =>
                        CategoryVotingFinishedWinnerMatches(args, "Geo", false, true)),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task EndCategoryVoting_TieRound2_UsesHostVoteAndBroadcastsTie()
        {
            var user = CreateUser(1);
            var (hub, _, _, _, groupProxy, _) = CreateHub(user);

            var game = new Game { RoomId = "room1", HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync("room1")).ReturnsAsync(game);

            _mockGameService
                .Setup(s => s.GetGameSettingsAsync("room1"))
                .ReturnsAsync(new GameSettings
                {
                    Difficulty = "medium",
                    QuestionCount = 10,
                    TimeLimitSeconds = 40
                });

            await hub.StartCategoryVoting("room1", new List<string> { "Geo", "History" });

            dynamic state = GetVotingState("room1");
            state.Round = 2;
            state.PlayerVotes.Clear();
            state.PlayerVotes[1] = "History"; // host vote
            state.PlayerVotes[2] = "Geo";

            await hub.EndCategoryVoting("room1");

            _mockGameService.Verify(
                s => s.UpdateGameSettingsAsync(
                    "room1",
                    It.Is<GameSettingsDto>(dto =>
                        dto.Category == "History" &&
                        dto.Difficulty == "medium" &&
                        dto.QuestionCount == 10 &&
                        dto.TimeLimitSeconds == 40)),
                Times.Once);

            groupProxy.Verify(
                g => g.SendCoreAsync(
                    "CategoryVotingFinished",
                    It.Is<object[]>(args =>
                        CategoryVotingFinishedWinnerMatches(args, "History", true, true)),
                    default),
                Times.Once);
        }

        // --------------------------------------------------
        // Helpers
        // --------------------------------------------------

        private dynamic GetVotingState(string roomId)
        {
            var field = typeof(GameHub).GetField("_categoryVotingStates",
                BindingFlags.Static | BindingFlags.NonPublic);
            var dict = field!.GetValue(null);
            var tryGetValue = dict!.GetType().GetMethod("TryGetValue");
            var args = new object[] { roomId, null! };
            var result = (bool)tryGetValue!.Invoke(dict, args)!;
            return result ? args[1] : null!;
        }

        private static bool CategoryVotingFinishedNoWinnerMatches(object[] args)
        {
            if (args.Length == 0 || args[0] is null) return false;

            var payload = args[0];
            var type = payload.GetType();

            var winnerProp = type.GetProperty("WinningCategory");
            var isFinalProp = type.GetProperty("IsFinal");
            if (winnerProp is null || isFinalProp is null) return false;

            var winner = winnerProp.GetValue(payload);
            var isFinal = (bool)isFinalProp.GetValue(payload)!;

            return winner == null && isFinal == false;
        }

        private static bool CategoryVotingFinishedWinnerMatches(
            object[] args,
            string expectedWinner,
            bool expectTie,
            bool expectFinal)
        {
            if (args.Length == 0 || args[0] is null) return false;

            var payload = args[0];
            var type = payload.GetType();

            var winnerProp = type.GetProperty("WinningCategory");
            var isFinalProp = type.GetProperty("IsFinal");
            var isTieProp = type.GetProperty("IsTie");

            if (winnerProp is null || isFinalProp is null) return false;

            var winner = winnerProp.GetValue(payload) as string;
            var isFinal = (bool)isFinalProp.GetValue(payload)!;
            var isTie = isTieProp != null && (bool)isTieProp.GetValue(payload)!;

            return winner == expectedWinner
                   && isFinal == expectFinal
                   && (!expectTie || isTie);
        }
    }
}
