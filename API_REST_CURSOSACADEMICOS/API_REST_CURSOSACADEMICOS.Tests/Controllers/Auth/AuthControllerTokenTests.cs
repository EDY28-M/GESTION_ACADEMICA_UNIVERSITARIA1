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
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthControllerTokenTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly AuthController _controller;

        public AuthControllerTokenTests()
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
        public async Task RefreshToken_WithValidToken_ReturnsOkResult()
        {
            // Arrange
            var refreshDto = new RefreshTokenDto
            {
                Token = "valid_token",
                RefreshToken = "valid_refresh_token"
            };

            var expectedResponse = new AuthResponseDto
            {
                Token = "new_jwt_token",
                RefreshToken = "new_refresh_token",
                Expiration = DateTime.UtcNow.AddHours(1)
            };

            _mockAuthService.Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RefreshToken(refreshDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
            response.Token.Should().Be("new_jwt_token");
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var refreshDto = new RefreshTokenDto
            {
                Token = "invalid_token",
                RefreshToken = "invalid_refresh_token"
            };

            _mockAuthService.Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenDto>()))
                .ReturnsAsync((AuthResponseDto?)null);

            // Act
            var result = await _controller.RefreshToken(refreshDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RefreshToken_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var refreshDto = new RefreshTokenDto
            {
                Token = "",
                RefreshToken = ""
            };

            _controller.ModelState.AddModelError("Token", "El token es requerido");

            // Act
            var result = await _controller.RefreshToken(refreshDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsOkResult()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer valid_token";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockAuthService.Setup(s => s.ValidateTokenAsync("valid_token"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ValidateToken();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer invalid_token";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _mockAuthService.Setup(s => s.ValidateTokenAsync("invalid_token"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ValidateToken();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ValidateToken_WithNoToken_ReturnsUnauthorized()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.ValidateToken();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
