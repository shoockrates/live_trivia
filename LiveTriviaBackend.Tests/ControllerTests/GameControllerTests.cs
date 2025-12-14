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
using live_trivia.Exceptions;
using System.Linq;

namespace live_trivia.Tests
{
    public class GamesControllerTests
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly Mock<IHubContext<GameHub>> _mockHubContext;
        // 1. ADD: Mock for the required third dependency
        private readonly Mock<IActiveGamesService> _mockActiveGamesService; 
        
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mockGameService = new Mock<IGameService>();
            _mockHubContext = new Mock<IHubContext<GameHub>>();
            
            // 2. INITIALIZE the new mock
            _mockActiveGamesService = new Mock<IActiveGamesService>(); 
            
            // 3. FIX: Pass all three required dependencies to the controller constructor
            _controller = new GamesController(
                _mockGameService.Object, 
                _mockHubContext.Object, 
                _mockActiveGamesService.Object // <--- ADDED MOCK
            );

            // 1. Create mocks
            var mockClients = new Mock<IHubClients>();
            var mockGroup = new Mock<IClientProxy>();
            
            // 2. Setup IClientProxy.SendCoreAsync (SendAsync extension method calls this internally)
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
        public async Task CreateGame_ReturnsUnauthorized_WhenPlayerIsNull()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.GetPlayerByIdAsync(1))
                .ReturnsAsync((Player?)null);

            var result = await _controller.CreateGame(roomId);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task StartGame_ReturnsOk_WhenSuccessful()
        {
            var roomId = "123";
            var gameDetails = new GameDetailsDto { RoomId = roomId };
            _mockGameService.Setup(s => s.StartGameAsync(roomId))
                .ReturnsAsync(true);
            _mockGameService.Setup(s => s.GetGameDetailsAsync(roomId))
                .ReturnsAsync(gameDetails);

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
        public async Task StartGame_ReturnsBadRequest_WhenNotEnoughQuestions()
        {
            var roomId = "123";
            var exception = new NotEnoughQuestionsException("Geography", 10);
            _mockGameService.Setup(s => s.StartGameAsync(roomId))
                .ThrowsAsync(exception);

            var result = await _controller.StartGame(roomId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value;
            Assert.NotNull(value);
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
        public async Task JoinGame_ReturnsNotFound_WhenGameIsNull()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync((Game?)null);

            var result = await _controller.JoinGame(roomId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task JoinGame_ReturnsUnauthorized_WhenPlayerIsNull()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.GetPlayerByIdAsync(1)).ReturnsAsync((Player?)null);

            var result = await _controller.JoinGame(roomId);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task JoinGame_ReturnsUnauthorized_WhenPlayerIdClaimMissing()
        {
            var roomId = "123";
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.JoinGame(roomId);

            Assert.IsType<UnauthorizedObjectResult>(result);
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

        [Fact]
        public async Task UpdateSettings_ReturnsNotFound_WhenGameIsNull()
        {
            var roomId = "123";
            var dto = new GameSettingsDto { Category = "History", Difficulty = "Medium", QuestionCount = 10, TimeLimitSeconds = 20 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync((Game?)null);

            var result = await _controller.UpdateSettings(roomId, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task NextQuestion_ReturnsOk_WhenSuccessful()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1, CurrentQuestionIndex = 0 };
            game.Questions.Add(new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"));
            game.Questions.Add(new Question("Test Question 2", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"));
            var gameDetails = new GameDetailsDto { RoomId = roomId };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockGameService.Setup(s => s.GetGameDetailsAsync(roomId)).ReturnsAsync(gameDetails);

            var result = await _controller.NextQuestion(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task NextQuestion_ReturnsNotFound_WhenGameIsNull()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync((Game?)null);

            var result = await _controller.NextQuestion(roomId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task NextQuestion_ReturnsUnauthorized_WhenPlayerIdClaimMissing()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.NextQuestion(roomId);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task NextQuestion_ReturnsForbid_WhenNotHost()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 99 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.NextQuestion(roomId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task NextQuestion_ReturnsOk_WhenGameFinished()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            game.Questions.Add(new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"));
            // Set to last question index so MoveNextQuestion will return false
            game.CurrentQuestionIndex = game.Questions.Count - 1;

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockGameService.Setup(s => s.CleanupGameAsync(roomId)).Returns(Task.CompletedTask);

            var result = await _controller.NextQuestion(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task SubmitAnswer_ReturnsOk_WhenSuccessful()
        {
            var roomId = "123";
            var player = new Player { Id = 1, Name = "TestPlayer" };
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            var game = new Game { RoomId = roomId };
            game.Questions.Add(question);
            game.GamePlayers.Add(new GamePlayer { PlayerId = 1, Player = player, GameRoomId = roomId });
            var request = new AnswerRequest { QuestionId = question.Id, SelectedAnswerIndexes = new List<int> { 0 }, TimeLeft = 10 };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _controller.SubmitAnswer(roomId, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            var playerAnswer = Assert.IsType<PlayerAnswer>(ok.Value);
            Assert.Equal(player.Id, playerAnswer.PlayerId);
            Assert.Equal(question.Id, playerAnswer.QuestionId);
        }

        [Fact]
        public async Task SubmitAnswer_ReturnsNotFound_WhenGameIsNull()
        {
            var roomId = "123";
            var request = new AnswerRequest { QuestionId = 1, SelectedAnswerIndexes = new List<int> { 0 }, TimeLeft = 10 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync((Game?)null);

            var result = await _controller.SubmitAnswer(roomId, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SubmitAnswer_ReturnsUnauthorized_WhenPlayerIdClaimMissing()
        {
            var roomId = "123";
            var request = new AnswerRequest { QuestionId = 1, SelectedAnswerIndexes = new List<int> { 0 }, TimeLeft = 10 };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.SubmitAnswer(roomId, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task SubmitAnswer_ReturnsNotFound_WhenPlayerNotInGame()
        {
            var roomId = "123";
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            var game = new Game { RoomId = roomId };
            game.Questions.Add(question);
            // No players in game
            var request = new AnswerRequest { QuestionId = question.Id, SelectedAnswerIndexes = new List<int> { 0 }, TimeLeft = 10 };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.SubmitAnswer(roomId, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SubmitAnswer_ReturnsNotFound_WhenQuestionNotFound()
        {
            var roomId = "123";
            var player = new Player { Id = 1, Name = "TestPlayer" };
            var game = new Game { RoomId = roomId };
            game.GamePlayers.Add(new GamePlayer { PlayerId = 1, Player = player, GameRoomId = roomId });
            // No questions in game
            var request = new AnswerRequest { QuestionId = 999, SelectedAnswerIndexes = new List<int> { 0 }, TimeLeft = 10 };

            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.SubmitAnswer(roomId, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void GetActiveGames_ReturnsOk_WithActiveGames()
        {
            var activeGames = new List<string> { "room1", "room2", "room3" };
            _mockActiveGamesService.Setup(s => s.GetActiveGameIds()).Returns(activeGames);

            var result = _controller.GetActiveGames();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(activeGames, ok.Value);
        }

        [Fact]
        public void GetActiveGames_ReturnsOk_WithEmptyList()
        {
            _mockActiveGamesService.Setup(s => s.GetActiveGameIds()).Returns(new List<string>());

            var result = _controller.GetActiveGames();

            var ok = Assert.IsType<OkObjectResult>(result);
            var games = Assert.IsType<List<string>>(ok.Value);
            Assert.Empty(games);
        }

        [Fact]
        public async Task DeleteGame_ReturnsOk_WhenSuccessful()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _mockGameService.Setup(s => s.CleanupGameAsync(roomId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteGame(roomId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeleteGame_ReturnsNotFound_WhenGameIsNull()
        {
            var roomId = "123";
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync((Game?)null);

            var result = await _controller.DeleteGame(roomId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteGame_ReturnsUnauthorized_WhenPlayerIdClaimMissing()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 1 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.DeleteGame(roomId);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task DeleteGame_ReturnsForbid_WhenNotHost()
        {
            var roomId = "123";
            var game = new Game { RoomId = roomId, HostPlayerId = 99 };
            _mockGameService.Setup(s => s.GetGameAsync(roomId)).ReturnsAsync(game);

            var result = await _controller.DeleteGame(roomId);

            Assert.IsType<ForbidResult>(result);
        }
    }
}