using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using live_trivia.Controllers;
using live_trivia.Interfaces;
using live_trivia.Dtos;
using System.Threading.Tasks;

namespace live_trivia.Tests.ControllerTests
{
    public class StatisticsControllerTests
    {
        private readonly Mock<IStatisticsService> _mockStatisticsService;
        private readonly StatisticsController _controller;

        public StatisticsControllerTests()
        {
            _mockStatisticsService = new Mock<IStatisticsService>();
            _controller = new StatisticsController(_mockStatisticsService.Object);

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
        public async Task GetPlayerStatistics_ReturnsOk_WhenValid()
        {
            var playerStats = new PlayerStatsResponse
            {
                TotalGamesPlayed = 5,
                TotalQuestionsAnswered = 50,
                TotalCorrectAnswers = 40,
                TotalScore = 1000,
                BestScore = 250,
                AccuracyPercentage = 80.0,
                AverageScore = 200.0
            };

            _mockStatisticsService.Setup(s => s.GetPlayerStatisticsAsync(1))
                .ReturnsAsync(playerStats);

            var result = await _controller.GetPlayerStatistics();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PlayerStatsResponse>(ok.Value);
            Assert.Equal(5, response.TotalGamesPlayed);
            Assert.Equal(80.0, response.AccuracyPercentage);
        }

        [Fact]
        public async Task GetPlayerStatistics_ReturnsUnauthorized_WhenPlayerIdMissing()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.GetPlayerStatistics();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetPlayerStatistics_ReturnsUnauthorized_WhenPlayerIdInvalid()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "invalid")
            }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;

            var result = await _controller.GetPlayerStatistics();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatistics_ReturnsOk_WhenValid()
        {
            var request = new UpdateStatsRequest
            {
                Category = "Geography",
                Score = 100,
                CorrectAnswers = 8,
                TotalQuestions = 10
            };

            _mockStatisticsService.Setup(s => s.UpdateGameStatisticsAsync(
                1, "Geography", 100, 8, 10))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateStatistics(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Statistics updated successfully", ok.Value);
        }

        [Fact]
        public async Task UpdateStatistics_ReturnsUnauthorized_WhenPlayerIdMissing()
        {
            var request = new UpdateStatsRequest
            {
                Category = "Geography",
                Score = 100,
                CorrectAnswers = 8,
                TotalQuestions = 10
            };

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var result = await _controller.UpdateStatistics(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatistics_ReturnsUnauthorized_WhenPlayerIdInvalid()
        {
            var request = new UpdateStatsRequest
            {
                Category = "Geography",
                Score = 100,
                CorrectAnswers = 8,
                TotalQuestions = 10
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "invalid")
            }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;

            var result = await _controller.UpdateStatistics(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatistics_CallsService_WithCorrectParameters()
        {
            var request = new UpdateStatsRequest
            {
                Category = "History",
                Score = 200,
                CorrectAnswers = 9,
                TotalQuestions = 10
            };

            _mockStatisticsService.Setup(s => s.UpdateGameStatisticsAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            await _controller.UpdateStatistics(request);

            _mockStatisticsService.Verify(s => s.UpdateGameStatisticsAsync(
                1, "History", 200, 9, 10), Times.Once);
        }
    }
}
