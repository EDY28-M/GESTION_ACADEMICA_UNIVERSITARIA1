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
    public class TrabajosEstudianteControllerEntregasTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosEstudianteController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly TrabajosEstudianteController _controller;

        public TrabajosEstudianteControllerEntregasTests()
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

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return estudiante;
        }

        [Fact]
        public async Task CreateEntrega_WithValidData_ReturnsCreated()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var dto = new EntregaCreateDto
            {
                IdTrabajo = 1,
                Comentario = "Mi entrega"
            };

            var entregaCreada = new EntregaDto
            {
                Id = 1,
                IdTrabajo = 1,
                IdEstudiante = estudiante.Id,
                Comentario = "Mi entrega",
                FechaEntrega = DateTime.Now
            };

            _mockTrabajoService.Setup(s => s.CrearEntregaAsync(It.IsAny<EntregaCreateDto>(), estudiante.Id))
                .ReturnsAsync((true, null, entregaCreada));

            // Act
            var result = await _controller.CreateEntrega(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var entrega = createdResult.Value.Should().BeOfType<EntregaDto>().Subject;
            entrega.Id.Should().Be(1);
        }

        [Fact]
        public async Task CreateEntrega_WithInvalidTrabajo_ReturnsBadRequest()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var dto = new EntregaCreateDto
            {
                IdTrabajo = 999,
                Comentario = "Mi entrega"
            };

            _mockTrabajoService.Setup(s => s.CrearEntregaAsync(It.IsAny<EntregaCreateDto>(), estudiante.Id))
                .ReturnsAsync((false, "Trabajo no encontrado", null));

            // Act
            var result = await _controller.CreateEntrega(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateEntrega_WithDuplicateEntrega_ReturnsBadRequest()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var dto = new EntregaCreateDto
            {
                IdTrabajo = 1,
                Comentario = "Mi entrega duplicada"
            };

            _mockTrabajoService.Setup(s => s.CrearEntregaAsync(It.IsAny<EntregaCreateDto>(), estudiante.Id))
                .ReturnsAsync((false, "Ya existe una entrega para este trabajo", null));

            // Act
            var result = await _controller.CreateEntrega(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateEntrega_WithUnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange - No valid claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var dto = new EntregaCreateDto
            {
                IdTrabajo = 1,
                Comentario = "Mi entrega"
            };

            // Act
            var result = await _controller.CreateEntrega(dto);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UpdateEntrega_WithValidData_ReturnsNoContent()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 1;
            var dto = new EntregaUpdateDto
            {
                Comentario = "Comentario actualizado"
            };

            _mockTrabajoService.Setup(s => s.ActualizarEntregaAsync(entregaId, It.IsAny<EntregaUpdateDto>(), estudiante.Id))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.UpdateEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateEntrega_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 999;
            var dto = new EntregaUpdateDto
            {
                Comentario = "Comentario actualizado"
            };

            _mockTrabajoService.Setup(s => s.ActualizarEntregaAsync(entregaId, It.IsAny<EntregaUpdateDto>(), estudiante.Id))
                .ReturnsAsync((true, false, "Entrega no encontrada"));

            // Act
            var result = await _controller.UpdateEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateEntrega_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 1;
            var dto = new EntregaUpdateDto
            {
                Comentario = "Comentario actualizado"
            };

            _mockTrabajoService.Setup(s => s.ActualizarEntregaAsync(entregaId, It.IsAny<EntregaUpdateDto>(), estudiante.Id))
                .ReturnsAsync((false, false, "No puedes modificar una entrega ya calificada"));

            // Act
            var result = await _controller.UpdateEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetEntrega_WithValidId_ReturnsOkWithEntrega()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 1;
            var entrega = new EntregaDto
            {
                Id = entregaId,
                IdTrabajo = 1,
                IdEstudiante = estudiante.Id,
                NombreEstudiante = "Test Estudiante",
                Comentario = "Mi entrega",
                FechaEntrega = DateTime.Now
            };

            _mockTrabajoService.Setup(s => s.GetEntregaAsync(entregaId))
                .ReturnsAsync(entrega);

            // Act
            var result = await _controller.GetEntrega(entregaId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedEntrega = okResult.Value.Should().BeOfType<EntregaDto>().Subject;
            returnedEntrega.Id.Should().Be(entregaId);
        }

        [Fact]
        public async Task GetEntrega_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 999;

            _mockTrabajoService.Setup(s => s.GetEntregaAsync(entregaId))
                .ReturnsAsync((EntregaDto?)null);

            // Act
            var result = await _controller.GetEntrega(entregaId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEntrega_WithOtherStudentEntrega_ReturnsForbid()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var entregaId = 1;
            var entrega = new EntregaDto
            {
                Id = entregaId,
                IdTrabajo = 1,
                IdEstudiante = 999, // Different student
                NombreEstudiante = "Otro Estudiante",
                Comentario = "Otra entrega",
                FechaEntrega = DateTime.Now
            };

            _mockTrabajoService.Setup(s => s.GetEntregaAsync(entregaId))
                .ReturnsAsync(entrega);

            // Act
            var result = await _controller.GetEntrega(entregaId);

            // Assert
            result.Result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetMiEntrega_WithValidTrabajo_ReturnsOkWithEntrega()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajoId = 1;

            // Create entrega in database
            var entregaDb = new TrabajoEntrega
            {
                Id = 1,
                IdTrabajo = trabajoId,
                IdEstudiante = estudiante.Id,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entregaDb);
            await _context.SaveChangesAsync();

            var entregaDto = new EntregaDto
            {
                Id = 1,
                IdTrabajo = trabajoId,
                IdEstudiante = estudiante.Id,
                Comentario = "Mi entrega",
                FechaEntrega = DateTime.Now
            };

            _mockTrabajoService.Setup(s => s.GetEntregaAsync(1))
                .ReturnsAsync(entregaDto);

            // Act
            var result = await _controller.GetMiEntrega(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedEntrega = okResult.Value.Should().BeOfType<EntregaDto>().Subject;
            returnedEntrega.IdTrabajo.Should().Be(trabajoId);
        }

        [Fact]
        public async Task GetMiEntrega_WithNoEntrega_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var trabajoId = 999;

            // No entrega exists for this trabajo and student

            // Act
            var result = await _controller.GetMiEntrega(trabajoId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
