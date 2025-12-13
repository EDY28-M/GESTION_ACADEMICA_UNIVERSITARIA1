using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthControllerLoginTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly AuthController _controller;

        public AuthControllerLoginTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserLookupService = new Mock<IUserLookupService>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockUserLookupService.Object
            );
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            var expectedResponse = new AuthResponseDto
            {
                Token = "jwt_token_here",
                RefreshToken = "refresh_token_here",
                Expiration = DateTime.UtcNow.AddHours(1),
                Usuario = new UsuarioDto
                {
                    Id = 1,
                    Email = "test@test.com",
                    Nombres = "Test",
                    Apellidos = "User",
                    Rol = "Administrador"
                }
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
            response.Token.Should().Be("jwt_token_here");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "wrong@test.com",
                Password = "WrongPassword"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync((AuthResponseDto?)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "",
                Password = "Password123!"
            };

            _controller.ModelState.AddModelError("Email", "El email es requerido");

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = ""
            };

            _controller.ModelState.AddModelError("Password", "La contrase�a es requerida");

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            _mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
    }
}
