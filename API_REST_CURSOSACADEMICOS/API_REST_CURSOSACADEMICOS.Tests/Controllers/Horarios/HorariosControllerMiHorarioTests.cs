using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Horarios
{
    public class HorariosControllerMiHorarioTests
    {
        private readonly Mock<IHorarioService> _mockHorarioService;
        private readonly GestionAcademicaContext _context;
        private readonly HorariosController _controller;

        public HorariosControllerMiHorarioTests()
        {
            _mockHorarioService = new Mock<IHorarioService>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new HorariosController(_mockHorarioService.Object, _context);
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

        private async Task SeedDocente()
        {
            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "docente@test.com"
            };
            await _context.Docentes.AddAsync(docente);
            await _context.SaveChangesAsync();
        }

        private async Task SeedEstudiante()
        {
            var estudiante = new Estudiante
            {
                Id = 1,
                Codigo = "EST001",
                Nombres = "María",
                Apellidos = "García",
                IdUsuario = 1,
                Correo = "estudiante@test.com"
            };
            await _context.Estudiantes.AddAsync(estudiante);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetMiHorario_AsDocente_ReturnsHorarios()
        {
            // Arrange
            await SeedDocente();
            SetupDocenteUser("docente@test.com");

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matemáticas", DiaSemanaTexto = "Lunes" }
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

            // Act
            var result = await _controller.GetMiHorario();

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMiHorario_AsEstudiante_ReturnsHorarios()
        {
            // Arrange
            await SeedEstudiante();
            SetupEstudianteUser(1);

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matemáticas", DiaSemanaTexto = "Lunes" },
                new HorarioDto { Id = 2, NombreCurso = "Física", DiaSemanaTexto = "Martes" }
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
            var estudiante = new Estudiante
            {
                Id = 2,
                Codigo = "EST002",
                Nombres = "Pedro",
                Apellidos = "López",
                Correo = "pedro@test.com"
                // IdUsuario is null or 0
            };
            await _context.Estudiantes.AddAsync(estudiante);
            await _context.SaveChangesAsync();

            SetupEstudianteUser(999, "pedro@test.com"); // userId no encontrado pero email sí

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, NombreCurso = "Matemáticas" }
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
