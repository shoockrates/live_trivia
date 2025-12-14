using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using LiveTriviaBackend.Controllers;
using live_trivia.Interfaces;
using live_trivia;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace live_trivia.Tests.ControllerTests
{
    public class QuestionsControllerTests
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly QuestionsController _controller;

        public QuestionsControllerTests()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _controller = new QuestionsController(_mockQuestionService.Object);
        }

        [Fact]
        public async Task GetAllQuestions_ReturnsOk_WithQuestions()
        {
            var questions = new List<Question>
            {
                new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"),
                new Question("Q2", new List<string> { "C", "D" }, new List<int> { 1 }, "Medium", "History")
            };

            _mockQuestionService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(questions);

            var result = await _controller.GetAllQuestions();

            var ok = Assert.IsType<OkObjectResult>(result);
            var qs = Assert.IsType<List<Question>>(ok.Value);
            Assert.Equal(2, qs.Count);
        }

        [Fact]
        public async Task GetAllQuestions_ReturnsOk_WithEmptyList()
        {
            _mockQuestionService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<Question>());

            var result = await _controller.GetAllQuestions();

            var ok = Assert.IsType<OkObjectResult>(result);
            var qs = Assert.IsType<List<Question>>(ok.Value);
            Assert.Empty(qs);
        }

        [Fact]
        public async Task GetRandom_ReturnsOk_WhenQuestionExists()
        {
            var question = new Question("Test Question", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography");
            _mockQuestionService.Setup(s => s.GetRandomAsync())
                .ReturnsAsync(question);

            var result = await _controller.GetRandom();

            var ok = Assert.IsType<OkObjectResult>(result);
            var q = Assert.IsType<Question>(ok.Value);
            Assert.Equal("Test Question", q.Text);
        }

        [Fact]
        public async Task GetRandom_ReturnsNotFound_WhenNoQuestions()
        {
            _mockQuestionService.Setup(s => s.GetRandomAsync())
                .ReturnsAsync((Question?)null);

            var result = await _controller.GetRandom();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetByCategory_ReturnsOk_WithQuestions()
        {
            var questions = new List<Question>
            {
                new Question("Q1", new List<string> { "A", "B" }, new List<int> { 0 }, "Easy", "Geography"),
                new Question("Q2", new List<string> { "C", "D" }, new List<int> { 1 }, "Easy", "Geography")
            };

            _mockQuestionService.Setup(s => s.GetByCategoryAsync("Geography"))
                .ReturnsAsync(questions);

            var result = await _controller.GetByCategory("geography");

            var ok = Assert.IsType<OkObjectResult>(result);
            var qs = Assert.IsType<List<Question>>(ok.Value);
            Assert.Equal(2, qs.Count);
        }

        [Fact]
        public async Task GetByCategory_CapitalizesCategory()
        {
            var questions = new List<Question>();
            _mockQuestionService.Setup(s => s.GetByCategoryAsync("Geography"))
                .ReturnsAsync(questions);

            await _controller.GetByCategory("geography");

            _mockQuestionService.Verify(s => s.GetByCategoryAsync("Geography"), Times.Once);
        }

        [Fact]
        public async Task LoadFromFile_ReturnsOk_WhenQuestionsLoaded()
        {
            _mockQuestionService.Setup(s => s.LoadFromFileAsync("questions.json"))
                .ReturnsAsync(5);

            // Setup user context for authorization
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await _controller.LoadFromFile("questions.json");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("5 questions loaded", ok.Value?.ToString() ?? "");
        }

        [Fact]
        public async Task LoadFromFile_ReturnsOk_WhenNoNewQuestions()
        {
            _mockQuestionService.Setup(s => s.LoadFromFileAsync("questions.json"))
                .ReturnsAsync(0);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await _controller.LoadFromFile("questions.json");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("No new questions to add.", ok.Value);
        }

        [Fact]
        public async Task LoadFromFile_ReturnsNotFound_WhenFileNotFound()
        {
            _mockQuestionService.Setup(s => s.LoadFromFileAsync("nonexistent.json"))
                .ThrowsAsync(new FileNotFoundException("File not found"));

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("playerId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await _controller.LoadFromFile("nonexistent.json");

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
