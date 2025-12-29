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
    public class TrabajosControllerCrudTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosController>> _mockLogger;
        private readonly Mock<GestionAcademicaContext> _mockContext;
        private readonly TrabajosController _controller;

        public TrabajosControllerCrudTests()
        {
            _mockTrabajoService = new Mock<ITrabajoService>();
            _mockLogger = new Mock<ILogger<TrabajosController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new GestionAcademicaContext(options);
            
            _controller = new TrabajosController(_mockTrabajoService.Object, context, _mockLogger.Object);
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
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            
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
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetTrabajo_WithValidId_ReturnsOkWithTrabajo()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 1;
            var trabajo = new TrabajoDto
            {
                Id = trabajoId,
                IdCurso = 1,
                Titulo = "Trabajo de Prueba",
                Descripcion = "Descripción del trabajo",
                FechaLimite = DateTime.Now.AddDays(7)
            };

            _mockTrabajoService.Setup(s => s.GetTrabajoAsync(trabajoId))
                .ReturnsAsync(trabajo);

            // Act
            var result = await _controller.GetTrabajo(trabajoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajo = okResult.Value.Should().BeOfType<TrabajoDto>().Subject;
            returnedTrabajo.Id.Should().Be(trabajoId);
            returnedTrabajo.Titulo.Should().Be("Trabajo de Prueba");
        }

        [Fact]
        public async Task GetTrabajo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 999;

            _mockTrabajoService.Setup(s => s.GetTrabajoAsync(trabajoId))
                .ReturnsAsync((TrabajoDto?)null);

            // Act
            var result = await _controller.GetTrabajo(trabajoId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTrabajosPorCurso_WithValidCurso_ReturnsOkWithList()
        {
            // Arrange
            SetupDocenteUser();
            var cursoId = 1;
            var trabajos = new List<TrabajoDto>
            {
                new TrabajoDto { Id = 1, IdCurso = cursoId, Titulo = "Trabajo 1" },
                new TrabajoDto { Id = 2, IdCurso = cursoId, Titulo = "Trabajo 2" }
            };

            _mockTrabajoService.Setup(s => s.GetTrabajosPorCursoAsync(cursoId))
                .ReturnsAsync(trabajos);

            // Act
            var result = await _controller.GetTrabajosPorCurso(cursoId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajos = okResult.Value.Should().BeAssignableTo<List<TrabajoDto>>().Subject;
            returnedTrabajos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTrabajosPorDocente_WithValidDocente_ReturnsOkWithList()
        {
            // Arrange
            SetupDocenteUser(1);
            var trabajos = new List<TrabajoDto>
            {
                new TrabajoDto { Id = 1, IdDocente = 1, Titulo = "Trabajo 1" },
                new TrabajoDto { Id = 2, IdDocente = 1, Titulo = "Trabajo 2" }
            };

            _mockTrabajoService.Setup(s => s.GetTrabajosPorDocenteAsync(1))
                .ReturnsAsync(trabajos);

            // Act
            var result = await _controller.GetTrabajosPorDocente();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTrabajos = okResult.Value.Should().BeAssignableTo<List<TrabajoDto>>().Subject;
            returnedTrabajos.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateTrabajo_WithValidData_ReturnsNoContent()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 1;
            var dto = new TrabajoUpdateDto
            {
                Titulo = "Trabajo Actualizado",
                FechaLimite = DateTime.Now.AddDays(14)
            };

            _mockTrabajoService.Setup(s => s.UpdateTrabajoAsync(trabajoId, It.IsAny<TrabajoUpdateDto>(), 1))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.UpdateTrabajo(trabajoId, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateTrabajo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 999;
            var dto = new TrabajoUpdateDto
            {
                Titulo = "Trabajo Actualizado"
            };

            _mockTrabajoService.Setup(s => s.UpdateTrabajoAsync(trabajoId, It.IsAny<TrabajoUpdateDto>(), 1))
                .ReturnsAsync((true, false, "No encontrado"));

            // Act
            var result = await _controller.UpdateTrabajo(trabajoId, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTrabajo_WithValidId_ReturnsNoContent()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 1;

            _mockTrabajoService.Setup(s => s.DeleteTrabajoAsync(trabajoId, 1))
                .ReturnsAsync((false, true, null));

            // Act
            var result = await _controller.DeleteTrabajo(trabajoId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteTrabajo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var trabajoId = 999;

            _mockTrabajoService.Setup(s => s.DeleteTrabajoAsync(trabajoId, 1))
                .ReturnsAsync((true, false, "No encontrado"));

            // Act
            var result = await _controller.DeleteTrabajo(trabajoId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTrabajo_WithUnauthorizedDocente_ReturnsUnauthorized()
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

            // Act
            var result = await _controller.DeleteTrabajo(1);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetClaims_ReturnsDebugInfo()
        {
            // Arrange
            SetupDocenteUser();

            // Act
            var result = _controller.GetClaims();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }
    }
}
