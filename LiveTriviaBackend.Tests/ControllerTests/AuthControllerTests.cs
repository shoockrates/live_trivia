using live_trivia.Controllers;
using live_trivia.Dtos;
using live_trivia.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace live_trivia.Tests.ControllerTests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenSuccessful()
        {
            var request = new RegisterRequest { Username = "user", Password = "pass" };
            var resultDto = new AuthResponse { Token = "token" };

            _mockAuthService.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(resultDto);

            var result = await _controller.Register(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resultDto, ok.Value);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_OnException()
        {
            var request = new RegisterRequest { Username = "user", Password = "pass" };

            _mockAuthService.Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new InvalidOperationException("error"));

            var result = await _controller.Register(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("error", badRequest.Value);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenSuccessful()
        {
            var request = new LoginRequest { Username = "user", Password = "pass" };
            var resultDto = new AuthResponse { Token = "token" };

            _mockAuthService.Setup(s => s.LoginAsync(request))
                .ReturnsAsync(resultDto);

            var result = await _controller.Login(request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resultDto, ok.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUnauthorizedAccess()
        {
            var request = new LoginRequest { Username = "user", Password = "pass" };

            _mockAuthService.Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new UnauthorizedAccessException("invalid"));

            var result = await _controller.Login(request);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("invalid", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_OnOtherException()
        {
            var request = new LoginRequest { Username = "user", Password = "pass" };

            _mockAuthService.Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new Exception("error"));

            var result = await _controller.Login(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("error", badRequest.Value);
        }
    }
}


