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
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthControllerPasswordTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthController _controller;

        public AuthControllerPasswordTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockConfiguration = new Mock<IConfiguration>();

            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GestionAcademicaContext(options);

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockLogger.Object,
                _context,
                _mockConfiguration.Object
            );
        }

        private void SetupUserClaims(string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Usuario")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task ChangePassword_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            SetupUserClaims("test@test.com");

            _mockAuthService.Setup(s => s.ChangePasswordAsync("test@test.com", It.IsAny<ChangePasswordDto>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(changePasswordDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword123!"
            };

            SetupUserClaims("test@test.com");

            _mockAuthService.Setup(s => s.ChangePasswordAsync("test@test.com", It.IsAny<ChangePasswordDto>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ChangePassword(changePasswordDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WithNoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.ChangePassword(changePasswordDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "",
                NewPassword = ""
            };

            SetupUserClaims("test@test.com");
            _controller.ModelState.AddModelError("CurrentPassword", "La contraseña actual es requerida");

            // Act
            var result = await _controller.ChangePassword(changePasswordDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Logout_WithValidEmail_ReturnsOkResult()
        {
            // Arrange
            var email = "test@test.com";

            _mockAuthService.Setup(s => s.RevokeTokenAsync(email))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Logout(email);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Logout_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var email = "";

            // Act
            var result = await _controller.Logout(email);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Logout_WhenServiceFails_ReturnsBadRequest()
        {
            // Arrange
            var email = "test@test.com";

            _mockAuthService.Setup(s => s.RevokeTokenAsync(email))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Logout(email);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
