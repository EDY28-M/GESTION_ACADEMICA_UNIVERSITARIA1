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
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthControllerUserInfoTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly AuthController _controller;

        public AuthControllerUserInfoTests()
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
        public async Task GetCurrentUser_WithValidUser_ReturnsUserInfo()
        {
            // Arrange
            var email = "test@test.com";
            SetupUserClaims(email);

            var expectedUser = new Usuario
            {
                Id = 1,
                Email = email,
                Nombres = "Test",
                Apellidos = "User",
                Rol = "Administrador",
                Estado = true,
                FechaCreacion = DateTime.Now,
                UltimoAcceso = DateTime.Now
            };

            _mockAuthService.Setup(s => s.GetUsuarioByEmailAsync(email))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var userDto = okResult.Value.Should().BeOfType<UsuarioDto>().Subject;
            userDto.Email.Should().Be(email);
            userDto.Nombres.Should().Be("Test");
        }

        [Fact]
        public async Task GetCurrentUser_WithNoEmailClaim_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var email = "notfound@test.com";
            SetupUserClaims(email);

            _mockAuthService.Setup(s => s.GetUsuarioByEmailAsync(email))
                .ReturnsAsync((Usuario?)null);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetCurrentUser_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var email = "test@test.com";
            SetupUserClaims(email);

            _mockAuthService.Setup(s => s.GetUsuarioByEmailAsync(email))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
    }
}
