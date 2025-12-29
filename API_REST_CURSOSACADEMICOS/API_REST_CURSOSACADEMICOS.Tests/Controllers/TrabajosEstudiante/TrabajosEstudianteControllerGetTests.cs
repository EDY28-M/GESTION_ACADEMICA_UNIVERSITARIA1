using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.TrabajosEstudiante
{
    public class TrabajosEstudianteControllerGetTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosEstudianteController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly TrabajosEstudianteController _controller;

        public TrabajosEstudianteControllerGetTests()
        {
            _mockTrabajoService = new Mock<ITrabajoService>();
            _mockLogger = new Mock<ILogger<TrabajosEstudianteController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GestionAcademicaContext(options);
            
            _controller = new TrabajosEstudianteController(_mockTrabajoService.Object, _context, _mockLogger.Object);
        }

        private async Task<Estudiante> SetupEstudianteUser(int usuarioId = 1)
        {
            // Create user and student
            var usuario = new Usuario
            {
                Id = usuarioId,
                Email = "estudiante@test.com",
                PasswordHash = "hash",
                Rol = "Estudiante"
            };
            _context.Usuarios.Add(usuario);

            var estudiante = new Estudiante
            {
                Id = usuarioId,
                IdUsuario = usuarioId,
                Codigo = $"EST00{usuarioId}",
                Nombres = "Test",
                Apellidos = "Estudiante",
                Dni = $"1234567{usuarioId}",
                CicloActual = 5
            };
            _context.Estudiantes.Add(estudiante);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return estudiante;
        }

        [Fact]
        public void Test_Endpoint_ReturnsOk()
        {
            // Act
            var result = _controller.Test();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTrabajosDisponibles_WithValidStudent_ReturnsOkWithList()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajos = new List<TrabajoSimpleDto>
            {
                new TrabajoSimpleDto { Id = 1, IdCurso = 1, Titulo = "Trabajo 1" },
                new TrabajoSimpleDto { Id = 2, IdCurso = 2, Titulo = "Trabajo 2" }
            };

            _mockTrabajoService.Setup(s => s.GetTrabajosDisponiblesAsync(estudiante.Id))
                .ReturnsAsync(trabajos);

            // Act
            var result = await _controller.GetTrabajosDisponibles();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajos = okResult.Value.Should().BeAssignableTo<List<TrabajoSimpleDto>>().Subject;
            returnedTrabajos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTrabajosDisponibles_WithNoTrabajos_ReturnsEmptyList()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            _mockTrabajoService.Setup(s => s.GetTrabajosDisponiblesAsync(estudiante.Id))
                .ReturnsAsync(new List<TrabajoSimpleDto>());

            // Act
            var result = await _controller.GetTrabajosDisponibles();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajos = okResult.Value.Should().BeAssignableTo<List<TrabajoSimpleDto>>().Subject;
            returnedTrabajos.Should().BeEmpty();
        }

        [Fact]
        public async Task GetTrabajosDisponibles_WithUnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange - No student setup, no valid claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Estudiante")
                // No NameIdentifier
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetTrabajosDisponibles();

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetTrabajosPorCurso_WithValidCurso_ReturnsOkWithList()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var cursoId = 1;
            var trabajos = new List<TrabajoSimpleDto>
            {
                new TrabajoSimpleDto { Id = 1, IdCurso = cursoId, Titulo = "Trabajo 1" },
                new TrabajoSimpleDto { Id = 2, IdCurso = cursoId, Titulo = "Trabajo 2" }
            };

            _mockTrabajoService.Setup(s => s.GetTrabajosPorCursoEstudianteAsync(cursoId, estudiante.Id))
                .ReturnsAsync(trabajos);

            // Act
            var result = await _controller.GetTrabajosPorCurso(cursoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajos = okResult.Value.Should().BeAssignableTo<List<TrabajoSimpleDto>>().Subject;
            returnedTrabajos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTrabajosPorCurso_WithStudentNotFound_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "999"), // Non-existent user
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetTrabajosPorCurso(1);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTrabajo_WithValidId_ReturnsOkWithTrabajo()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajoId = 1;
            var trabajo = new TrabajoDto
            {
                Id = trabajoId,
                IdCurso = 1,
                Titulo = "Trabajo de Prueba",
                Descripcion = "Descripción del trabajo",
                FechaLimite = DateTime.Now.AddDays(7)
            };

            _mockTrabajoService.Setup(s => s.GetTrabajoParaEstudianteAsync(trabajoId, estudiante.Id))
                .ReturnsAsync(trabajo);

            // Create trabajo in database for the check
            var trabajoDb = new TrabajoEncargado
            {
                Id = trabajoId,
                IdCurso = 1,
                IdDocente = 1,
                Titulo = "Trabajo de Prueba",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajoDb);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetTrabajo(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajo = okResult.Value.Should().BeOfType<TrabajoDto>().Subject;
            returnedTrabajo.Id.Should().Be(trabajoId);
        }

        [Fact]
        public async Task GetTrabajo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajoId = 999;

            // No trabajo exists with this ID

            // Act
            var result = await _controller.GetTrabajo(trabajoId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTrabajo_WithNoAccess_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajoId = 1;

            // Create trabajo in database
            var trabajoDb = new TrabajoEncargado
            {
                Id = trabajoId,
                IdCurso = 1,
                IdDocente = 1,
                Titulo = "Trabajo de Prueba",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajoDb);
            await _context.SaveChangesAsync();

            _mockTrabajoService.Setup(s => s.GetTrabajoParaEstudianteAsync(trabajoId, estudiante.Id))
                .ReturnsAsync((TrabajoDto?)null);

            // Act
            var result = await _controller.GetTrabajo(trabajoId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
