using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Horarios
{
    public class HorariosControllerMiHorarioTests
    {
        private readonly Mock<IHorarioService> _mockHorarioService;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly HorariosController _controller;

        public HorariosControllerMiHorarioTests()
        {
            _mockHorarioService = new Mock<IHorarioService>();
            _mockUserLookupService = new Mock<IUserLookupService>();
            _controller = new HorariosController(_mockHorarioService.Object, _mockUserLookupService.Object);
        }

        private void SetupDocenteUser(string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Docente")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupEstudianteUser(int userId, string? email = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            if (email != null)
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupNoRoleUser()
        {
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
        }

        [Fact]
        public async Task GetMiHorario_AsDocente_ReturnsHorarios()
        {
            // Arrange
            SetupDocenteUser("docente@test.com");
            _mockUserLookupService
                .Setup(s => s.GetDocenteIdByEmailAsync("docente@test.com"))
                .ReturnsAsync(1);

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matem�ticas", DiaSemanaTexto = "Lunes" }
            };

            _mockHorarioService.Setup(s => s.ObtenerPorDocenteAsync(1)).ReturnsAsync(horarios);

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedHorarios = okResult.Value.Should().BeAssignableTo<IEnumerable<HorarioDto>>().Subject;
            returnedHorarios.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetMiHorario_AsDocente_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser("notfound@test.com");
            _mockUserLookupService
                .Setup(s => s.GetDocenteIdByEmailAsync("notfound@test.com"))
                .ReturnsAsync((int?)null);

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMiHorario_AsEstudiante_ReturnsHorarios()
        {
            // Arrange
            SetupEstudianteUser(1);
            _mockUserLookupService
                .Setup(s => s.GetEstudianteIdByUsuarioIdAsync(1))
                .ReturnsAsync(1);

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matem�ticas", DiaSemanaTexto = "Lunes" },
                new HorarioDto { Id = 2, NombreCurso = "F�sica", DiaSemanaTexto = "Martes" }
            };

            _mockHorarioService.Setup(s => s.ObtenerPorEstudianteAsync(1)).ReturnsAsync(horarios);

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedHorarios = okResult.Value.Should().BeAssignableTo<IEnumerable<HorarioDto>>().Subject;
            returnedHorarios.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMiHorario_AsEstudiante_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(999);
            _mockUserLookupService
                .Setup(s => s.GetEstudianteIdByUsuarioIdAsync(999))
                .ReturnsAsync((int?)null);

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMiHorario_WithNoRole_ReturnsUnauthorized()
        {
            // Arrange
            SetupNoRoleUser();

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetMiHorario_WithInvalidRole_ReturnsBadRequest()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "OtroRol")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMiHorario_AsEstudiante_FallbackToEmail_ReturnsHorarios()
        {
            // Arrange
            SetupEstudianteUser(999, "pedro@test.com"); // userId no encontrado pero email s�
            _mockUserLookupService
                .Setup(s => s.GetEstudianteIdByUsuarioIdAsync(999))
                .ReturnsAsync((int?)null);
            _mockUserLookupService
                .Setup(s => s.GetEstudianteIdByEmailAsync("pedro@test.com"))
                .ReturnsAsync(2);

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matem�ticas" }
            };

            _mockHorarioService.Setup(s => s.ObtenerPorEstudianteAsync(2)).ReturnsAsync(horarios);

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedHorarios = okResult.Value.Should().BeAssignableTo<IEnumerable<HorarioDto>>().Subject;
            returnedHorarios.Should().HaveCount(1);
        }
    }
}
