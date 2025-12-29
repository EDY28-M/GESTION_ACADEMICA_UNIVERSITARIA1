using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Trabajos
{
    public class TrabajosControllerEntregasTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly TrabajosController _controller;

        public TrabajosControllerEntregasTests()
        {
            _mockTrabajoService = new Mock<ITrabajoService>();
            _mockLogger = new Mock<ILogger<TrabajosController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GestionAcademicaContext(options);
            
            _controller = new TrabajosController(_mockTrabajoService.Object, _context, _mockLogger.Object);
        }

        private void SetupDocenteUser(int docenteId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, docenteId.ToString()),
                new Claim(ClaimTypes.Role, "Docente"),
                new Claim("DocenteId", docenteId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
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

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetEntregasPorTrabajo_WithValidTrabajo_ReturnsOkWithList()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 1;
            var entregas = new List<EntregaDto>
            {
                new EntregaDto
                {
                    Id = 1,
                    IdTrabajo = trabajoId,
                    IdEstudiante = 1,
                    NombreEstudiante = "Juan Pérez",
                    FechaEntrega = DateTime.Now.AddDays(-1)
                },
                new EntregaDto
                {
                    Id = 2,
                    IdTrabajo = trabajoId,
                    IdEstudiante = 2,
                    NombreEstudiante = "María García",
                    FechaEntrega = DateTime.Now.AddDays(-2)
                }
            };

            _mockTrabajoService.Setup(s => s.GetEntregasPorTrabajoAsync(trabajoId, 1))
                .ReturnsAsync(entregas);

            // Act
            var result = await _controller.GetEntregasPorTrabajo(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedEntregas = okResult.Value.Should().BeAssignableTo<List<EntregaDto>>().Subject;
            returnedEntregas.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetEntregasPorTrabajo_AsAdmin_ReturnsOkWithList()
        {
            // Arrange
            SetupAdminUser();
            var trabajoId = 1;
            var entregas = new List<EntregaDto>
            {
                new EntregaDto
                {
                    Id = 1,
                    IdTrabajo = trabajoId,
                    IdEstudiante = 1,
                    NombreEstudiante = "Juan Pérez"
                }
            };

            _mockTrabajoService.Setup(s => s.GetEntregasPorTrabajoAsync(trabajoId, 0))
                .ReturnsAsync(entregas);

            // Act
            var result = await _controller.GetEntregasPorTrabajo(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedEntregas = okResult.Value.Should().BeAssignableTo<List<EntregaDto>>().Subject;
            returnedEntregas.Should().HaveCount(1);
        }

        [Fact]
        public async Task CalificarEntrega_WithValidData_ReturnsNoContent()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 1;
            var dto = new CalificarEntregaDto
            {
                Calificacion = 18.5m,
                Observaciones = "Buen trabajo"
            };

            _mockTrabajoService.Setup(s => s.CalificarEntregaAsync(entregaId, dto, 1))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.CalificarEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task CalificarEntrega_WithInvalidEntregaId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 999;
            var dto = new CalificarEntregaDto
            {
                Calificacion = 18.5m
            };

            _mockTrabajoService.Setup(s => s.CalificarEntregaAsync(entregaId, dto, 1))
                .ReturnsAsync((true, false, "No encontrado"));

            // Act
            var result = await _controller.CalificarEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CalificarEntrega_WithInvalidCalificacion_ReturnsBadRequest()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 1;
            var dto = new CalificarEntregaDto
            {
                Calificacion = 18.5m
            };

            _mockTrabajoService.Setup(s => s.CalificarEntregaAsync(entregaId, dto, 1))
                .ReturnsAsync((false, false, "La calificación debe estar entre 0 y 20"));

            // Act
            var result = await _controller.CalificarEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CalificarEntrega_WithUnauthorizedDocente_ReturnsUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Docente")
                // Sin DocenteId claim
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var dto = new CalificarEntregaDto
            {
                Calificacion = 18.5m
            };

            // Act
            var result = await _controller.CalificarEntrega(1, dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetEntregasPorTrabajo_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 1;

            _mockTrabajoService.Setup(s => s.GetEntregasPorTrabajoAsync(trabajoId, 1))
                .ReturnsAsync(new List<EntregaDto>());

            // Act
            var result = await _controller.GetEntregasPorTrabajo(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedEntregas = okResult.Value.Should().BeAssignableTo<List<EntregaDto>>().Subject;
            returnedEntregas.Should().BeEmpty();
        }

        [Fact]
        public async Task CalificarEntrega_WithZeroCalificacion_ReturnsNoContent()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 1;
            var dto = new CalificarEntregaDto
            {
                Calificacion = 0,
                Observaciones = "No cumple con los requisitos"
            };

            _mockTrabajoService.Setup(s => s.CalificarEntregaAsync(entregaId, dto, 1))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.CalificarEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task CalificarEntrega_WithMaxCalificacion_ReturnsNoContent()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 1;
            var dto = new CalificarEntregaDto
            {
                Calificacion = 20,
                Observaciones = "Excelente trabajo"
            };

            _mockTrabajoService.Setup(s => s.CalificarEntregaAsync(entregaId, dto, 1))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.CalificarEntrega(entregaId, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }
    }
}
