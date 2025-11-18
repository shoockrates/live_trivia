using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using live_trivia.Controllers;
using live_trivia.Services;
using live_trivia.Interfaces;
using live_trivia.Hubs;
using live_trivia.Dtos;
using live_trivia;

namespace live_trivia.Tests
{
    public class GamesControllerTests
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly Mock<IHubContext<GameHub>> _mockHubContext;
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mockGameService = new Mock<IGameService>();
            _mockHubContext = new Mock<IHubContext<GameHub>>();
            _controller = new GamesController(_mockGameService.Object, _mockHubContext.Object);

            // 1. Create mocks
            var mockClients = new Mock<IHubClients>();
            var mockGroup = new Mock<IClientProxy>();
            
            // 2. Setup IClientProxy.SendCoreAsync instead of SendAsync
            mockGroup
                .Setup(g => g.SendCoreAsync(
                    It.IsAny<string>(), 
                    It.IsAny<object?[]>(), 
                    default))
                .Returns(Task.CompletedTask);
            
            // 3. Setup Clients.Group to return the mock group
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);
            
            // 4. Setup hub context to return mock clients
            _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            

            // Mock User with Claims for authorization
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetGame_ReturnsOk_WhenGameExists()
        {
            var roomId = "123";
            var dto = new GameDetailsDto { RoomId = roomId };
            _mockGameService.Setup(s => s.GetGameDetailsAsync(roomId))
                .ReturnsAsync(dto);

            var result = await _controller.GetGame(roomId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
        {
            _mockGameService.Setup(s => s.GetGameDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync((GameDetailsDto?)null);

            var result = await _controller.GetGame("nonexistent");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateGame_ReturnsCreated_WhenPlayerValid()
        {
            var roomId = "123";
            var player = new Player { Id = 1, Name = "TestPlayer" };
            var game = new Game { RoomId = roomId, HostPlayer = player };

            _mockGameService.Setup(s => s.GetPlayerByIdAsync(1))
                .ReturnsAsync(player);
            _mockGameService.Setup(s => s.CreateGameAsync(roomId, player))
                .ReturnsAsync(game);

            var result = await _controller.CreateGame(roomId);

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal(game, created.Value);
        }

        [Fact]
        public async Task CreateGame_ReturnsUnauthorized_WhenPlayerMissing()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // no claims
            var result = await _controller.CreateGame("123");
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task StartGame_ReturnsOk_WhenSuccessful()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.StartGameAsync(roomId))
                .ReturnsAsync(true);

            var result = await _controller.StartGame(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task StartGame_ReturnsBadRequest_WhenFail()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.StartGameAsync(roomId))
                .ReturnsAsync(false);

            var result = await _controller.StartGame(roomId);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task JoinGame_ReturnsOk_WhenPlayerAdded()
        {
            var roomId = "123";

            var player = new Player { Id = 1, Name = "TestPlayer" };
            var game = new Game { RoomId = roomId, HostPlayerId = 1 }; // Player not yet in GamePlayers

            // Mock service
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.GetPlayerByIdAsync(1)).ReturnsAsync(player);
            _mockGameService.Setup(s => s.AddExistingPlayerToGameAsync(game, player))
                            .Returns(Task.CompletedTask);

            var result = await _controller.JoinGame(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(player, ok.Value);
        }

        [Fact]
        public async Task JoinGame_ReturnsBadRequest_WhenPlayerAlreadyInGame()
        {
            var roomId = "123";
            var player = new Player { Id = 1, Name = "TestPlayer" };
            var game = new Game { RoomId = roomId, GamePlayers = { new GamePlayer { PlayerId = 1, Player = player } } };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.JoinGame(roomId);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetSettings_ReturnsOk_WhenSettingsExist()
        {
            var roomId = "123";
            var settings = new GameSettings { Category = "Geography", Difficulty = "Easy", QuestionCount = 5, TimeLimitSeconds = 15 };
            _mockGameService.Setup(s => s.GetGameSettingsAsync(roomId)).ReturnsAsync(settings);

            var result = await _controller.GetSettings(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<GameSettingsDto>(ok.Value);
            Assert.Equal(settings.Category, dto.Category);
        }

        [Fact]
        public async Task GetSettings_ReturnsNotFound_WhenSettingsMissing()
        {
            _mockGameService.Setup(s => s.GetGameSettingsAsync(It.IsAny<string>())).ReturnsAsync((GameSettings?)null);
            var result = await _controller.GetSettings("123");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSettings_ReturnsOk_WhenHost()
        {
            var roomId = "123";
            var player = new Player { Id = 1 };
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            var dto = new GameSettingsDto { Category = "History", Difficulty = "Medium", QuestionCount = 10, TimeLimitSeconds = 20 };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.UpdateGameSettingsAsync(roomId, dto)).ReturnsAsync(new GameSettings { GameRoomId = roomId });

            var result = await _controller.UpdateSettings(roomId, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSettings_ReturnsForbid_WhenNotHost()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 99 };
            var dto = new GameSettingsDto { Category = "History", Difficulty = "Medium", QuestionCount = 10, TimeLimitSeconds = 20 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.UpdateSettings(roomId, dto);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
