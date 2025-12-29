using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Services;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth
{
    public class AuthPasswordResetTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly Mock<IPasswordResetService> _mockPasswordResetService;
        private readonly AuthController _controller;

        public AuthPasswordResetTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserLookupService = new Mock<IUserLookupService>();
            _mockPasswordResetService = new Mock<IPasswordResetService>();

            _mockConfiguration.Setup(c => c["JwtSettings:SecretKey"])
                .Returns("SuperSecretKeyForTestingPurposesAtLeast32Characters");

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

        #region ForgotPassword Tests

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ReturnsOk()
        {
            // Arrange
            var request = new ForgotPasswordRequestDto
            {
                Email = "estudiante@test.com",
                TipoUsuario = "Estudiante"
            };

            var expectedResponse = new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "Si el correo existe, recibirás instrucciones para recuperar tu contraseña."
            };

            _mockPasswordResetService.Setup(s => s.RequestPasswordResetAsync("estudiante@test.com", "Estudiante"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ForgotPassword(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ForgotPasswordResponseDto>().Subject;
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_StillReturnsOk()
        {
            // Arrange - Siempre devuelve OK para no revelar si el email existe
            var request = new ForgotPasswordRequestDto
            {
                Email = "noexiste@test.com",
                TipoUsuario = "Estudiante"
            };

            var expectedResponse = new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "Si el correo existe, recibirás instrucciones para recuperar tu contraseña."
            };

            _mockPasswordResetService.Setup(s => s.RequestPasswordResetAsync("noexiste@test.com", "Estudiante"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ForgotPassword(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ForgotPasswordResponseDto>().Subject;
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ForgotPassword_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new ForgotPasswordRequestDto
            {
                Email = "email-invalido",
                TipoUsuario = "Estudiante"
            };

            _controller.ModelState.AddModelError("Email", "Formato de email inválido");

            // Act
            var result = await _controller.ForgotPassword(request, _mockPasswordResetService.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ForgotPassword_ForDocente_CallsServiceWithDocente()
        {
            // Arrange
            var request = new ForgotPasswordRequestDto
            {
                Email = "docente@test.com",
                TipoUsuario = "Docente"
            };

            var expectedResponse = new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "Instrucciones enviadas"
            };

            _mockPasswordResetService.Setup(s => s.RequestPasswordResetAsync("docente@test.com", "Docente"))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.ForgotPassword(request, _mockPasswordResetService.Object);

            // Assert
            _mockPasswordResetService.Verify(s => s.RequestPasswordResetAsync("docente@test.com", "Docente"), Times.Once);
        }

        #endregion

        #region ValidateResetToken Tests

        [Fact]
        public async Task ValidateResetToken_WithValidToken_ReturnsValid()
        {
            // Arrange
            var request = new ValidateTokenDto
            {
                Token = "valid-reset-token-123"
            };

            var expectedResponse = new ValidateTokenResponseDto
            {
                Valid = true,
                Email = "usuario@test.com",
                Message = "Token válido"
            };

            _mockPasswordResetService.Setup(s => s.ValidateTokenAsync("valid-reset-token-123"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ValidateResetToken(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ValidateTokenResponseDto>().Subject;
            response.Valid.Should().BeTrue();
            response.Email.Should().Be("usuario@test.com");
        }

        [Fact]
        public async Task ValidateResetToken_WithExpiredToken_ReturnsInvalid()
        {
            // Arrange
            var request = new ValidateTokenDto
            {
                Token = "expired-token"
            };

            var expectedResponse = new ValidateTokenResponseDto
            {
                Valid = false,
                Message = "El token ha expirado"
            };

            _mockPasswordResetService.Setup(s => s.ValidateTokenAsync("expired-token"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ValidateResetToken(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ValidateTokenResponseDto>().Subject;
            response.Valid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateResetToken_WithEmptyToken_ReturnsInvalid()
        {
            // Arrange
            var request = new ValidateTokenDto
            {
                Token = ""
            };

            // Act
            var result = await _controller.ValidateResetToken(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ValidateTokenResponseDto>().Subject;
            response.Valid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateResetToken_WithNullToken_ReturnsInvalid()
        {
            // Arrange
            var request = new ValidateTokenDto
            {
                Token = null!
            };

            // Act
            var result = await _controller.ValidateResetToken(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ValidateTokenResponseDto>().Subject;
            response.Valid.Should().BeFalse();
        }

        #endregion

        #region ResetPassword Tests

        [Fact]
        public async Task ResetPassword_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                Token = "valid-token",
                NewPassword = "NewSecurePassword123!"
            };

            var expectedResponse = new ResetPasswordResponseDto
            {
                Success = true,
                Message = "Contraseña actualizada correctamente"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync("valid-token", "NewSecurePassword123!"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ResetPassword(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ResetPasswordResponseDto>().Subject;
            response.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsFailed()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                Token = "invalid-token",
                NewPassword = "NewSecurePassword123!"
            };

            var expectedResponse = new ResetPasswordResponseDto
            {
                Success = false,
                Message = "Token inválido o expirado"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync("invalid-token", "NewSecurePassword123!"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ResetPassword(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ResetPasswordResponseDto>().Subject;
            response.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ResetPassword_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                Token = "valid-token",
                NewPassword = "123" // Too short
            };

            _controller.ModelState.AddModelError("NewPassword", "La contraseña debe tener al menos 6 caracteres");

            // Act
            var result = await _controller.ResetPassword(request, _mockPasswordResetService.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ResetPassword_WhenServiceThrows_ReturnsOkWithFailed()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                Token = "valid-token",
                NewPassword = "NewSecurePassword123!"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync("valid-token", "NewSecurePassword123!"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ResetPassword(request, _mockPasswordResetService.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ResetPasswordResponseDto>().Subject;
            response.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ResetPassword_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var request = new ResetPasswordDto
            {
                Token = "my-reset-token",
                NewPassword = "MyNewPassword!"
            };

            var expectedResponse = new ResetPasswordResponseDto
            {
                Success = true,
                Message = "Contraseña actualizada"
            };

            _mockPasswordResetService.Setup(s => s.ResetPasswordAsync("my-reset-token", "MyNewPassword!"))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.ResetPassword(request, _mockPasswordResetService.Object);

            // Assert
            _mockPasswordResetService.Verify(s => s.ResetPasswordAsync("my-reset-token", "MyNewPassword!"), Times.Once);
        }

        #endregion
    }
}
