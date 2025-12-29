using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Docentes
{
    public class DocentesControllerCrudTests
    {
        private readonly Mock<ILogger<DocentesController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly DocentesController _controller;

        public DocentesControllerCrudTests()
        {
            _mockLogger = new Mock<ILogger<DocentesController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            var asistenciaServiceMock = new Mock<IAsistenciaService>();
            var docentesService = new DocentesService(_context, asistenciaServiceMock.Object);
            _controller = new DocentesController(docentesService, _mockLogger.Object);
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Administrador")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private async Task SeedDocentes()
        {
            var docentes = new List<Docente>
            {
                new Docente
                {
                    Id = 1,
                    Nombres = "Juan",
                    Apellidos = "P�rez",
                    Profesion = "Ingeniero de Sistemas",
                    Correo = "juan.perez@test.com"
                },
                new Docente
                {
                    Id = 2,
                    Nombres = "Mar�a",
                    Apellidos = "Garc�a",
                    Profesion = "Matem�tica",
                    Correo = "maria.garcia@test.com"
                }
            };

            await _context.Docentes.AddRangeAsync(docentes);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetDocentes_ReturnsAllDocentes()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.GetDocentes();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var docentes = okResult.Value.Should().BeAssignableTo<IEnumerable<DocenteDto>>().Subject;
            docentes.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetDocente_WithValidId_ReturnsDocente()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.GetDocente(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var docente = okResult.Value.Should().BeOfType<DocenteDto>().Subject;
            docente.Nombres.Should().Be("Juan");
        }

        [Fact]
        public async Task GetDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.GetDocente(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task PostDocente_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAdminUser();
            var docenteDto = new DocenteCreateDto
            {
                Nombres = "Carlos",
                Apellidos = "L�pez",
                Profesion = "Ingeniero Civil",
                Correo = "carlos.lopez@test.com"
            };

            // Act
            var result = await _controller.PostDocente(docenteDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var docente = createdResult.Value.Should().BeOfType<DocenteDto>().Subject;
            docente.Nombres.Should().Be("Carlos");
        }

        [Fact]
        public async Task PostDocente_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var docenteDto = new DocenteCreateDto
            {
                Nombres = "Carlos",
                Apellidos = "L�pez",
                Correo = "juan.perez@test.com" // Email duplicado
            };

            // Act
            var result = await _controller.PostDocente(docenteDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PutDocente_WithValidData_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var updateDto = new DocenteUpdateDto
            {
                Nombres = "Juan Carlos",
                Apellidos = "P�rez",
                Profesion = "Ingeniero de Software",
                Correo = "juan.perez@test.com"
            };

            // Act
            var result = await _controller.PutDocente(1, updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task PutDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var updateDto = new DocenteUpdateDto
            {
                Nombres = "Test",
                Apellidos = "Test"
            };

            // Act
            var result = await _controller.PutDocente(999, updateDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteDocente_WithValidId_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.DeleteDocente(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var docente = await _context.Docentes.FindAsync(1);
            docente.Should().BeNull();
        }

        [Fact]
        public async Task DeleteDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.DeleteDocente(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteDocente_WithAssignedCourses_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Agregar un curso asignado al docente
            _context.Cursos.Add(new Curso
            {
                Id = 1,
                NombreCurso = "Matem�ticas",
                Creditos = 4,
                HorasSemanal = 4,
                Ciclo = 1,
                IdDocente = 1
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteDocente(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
