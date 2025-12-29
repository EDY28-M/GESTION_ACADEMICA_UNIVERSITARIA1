using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthDocenteLoginTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly AuthController _controller;

        public AuthDocenteLoginTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserLookupService = new Mock<IUserLookupService>();

            // Setup JWT configuration
            _mockConfiguration.Setup(c => c["JwtSettings:SecretKey"])
                .Returns("SuperSecretKeyForTestingPurposesAtLeast32Characters");
            _mockConfiguration.Setup(c => c["JwtSettings:Issuer"])
                .Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["JwtSettings:Audience"])
                .Returns("TestAudience");
            _mockConfiguration.Setup(c => c["JwtSettings:ExpirationMinutes"])
                .Returns("30");

            _controller = new AuthController(
                _mockAuthService.Object, 
                _mockLogger.Object, 
                _mockConfiguration.Object,
                _mockUserLookupService.Object);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task LoginDocente_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthDocenteResponseDto>().Subject;
            response.Token.Should().NotBeNullOrEmpty();
            response.Correo.Should().Be("docente@test.com");
        }

        [Fact]
        public async Task LoginDocente_WithInvalidEmail_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "noexiste@test.com",
                Password = "Password123!"
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("noexiste@test.com"))
                .ReturnsAsync((Docente?)null);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task LoginDocente_WithWrongPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "WrongPassword!"
            };

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task LoginDocente_WithNoPasswordConfigured_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = null
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task LoginDocente_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "",
                Password = ""
            };

            _controller.ModelState.AddModelError("Correo", "El correo es requerido");

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task LoginDocente_ReturnsCorrectNombreCompleto()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "maria@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 2,
                Nombres = "María",
                Apellidos = "González García",
                Correo = "maria@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("maria@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthDocenteResponseDto>().Subject;
            response.NombreCompleto.Should().Be("María González García");
        }

        [Fact]
        public async Task LoginDocente_ReturnsRefreshToken()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthDocenteResponseDto>().Subject;
            response.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginDocente_ReturnsExpirationTime()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthDocenteResponseDto>().Subject;
            response.Expiracion.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task LoginDocente_ReturnsDocenteId()
        {
            // Arrange
            var loginDto = new LoginDocenteDto
            {
                Correo = "docente@test.com",
                Password = "Password123!"
            };

            var docente = new Docente
            {
                Id = 42,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            };

            _mockUserLookupService.Setup(s => s.GetDocenteByEmailAsync("docente@test.com"))
                .ReturnsAsync(docente);

            // Act
            var result = await _controller.LoginDocente(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AuthDocenteResponseDto>().Subject;
            response.Id.Should().Be(42);
        }
    }
}
