using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Asistencias
{
    public class AsistenciasControllerDocenteTests
    {
        private readonly Mock<IAsistenciaService> _mockAsistenciaService;
        private readonly Mock<ILogger<AsistenciasController>> _mockLogger;
        private readonly AsistenciasController _controller;

        public AsistenciasControllerDocenteTests()
        {
            _mockAsistenciaService = new Mock<IAsistenciaService>();
            _mockLogger = new Mock<ILogger<AsistenciasController>>();
            _controller = new AsistenciasController(_mockAsistenciaService.Object, _mockLogger.Object);
        }

        private void SetupDocenteUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Docente")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
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

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task RegistrarAsistencia_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var dto = new RegistrarAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                Fecha = DateTime.Now,
                Presente = true,
                TipoClase = "Teoría"
            };

            var asistenciaDto = new AsistenciaDto
            {
                Id = 1,
                IdEstudiante = 1,
                IdCurso = 1,
                Fecha = DateTime.Now,
                Presente = true,
                TipoClase = "Teoría"
            };

            _mockAsistenciaService.Setup(s => s.RegistrarAsistenciaAsync(dto))
                .ReturnsAsync(asistenciaDto);

            // Act
            var result = await _controller.RegistrarAsistencia(dto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var asistencia = okResult.Value.Should().BeOfType<AsistenciaDto>().Subject;
            asistencia.Id.Should().Be(1);
        }

        [Fact]
        public async Task RegistrarAsistencia_WithInvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            SetupDocenteUser();

            var dto = new RegistrarAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                Fecha = DateTime.Now,
                Presente = true,
                TipoClase = "Teoría"
            };

            _mockAsistenciaService.Setup(s => s.RegistrarAsistenciaAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Ya existe asistencia"));

            // Act
            var result = await _controller.RegistrarAsistencia(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RegistrarAsistenciasMasivas_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var dto = new RegistrarAsistenciasMasivasDto
            {
                IdCurso = 1,
                Fecha = DateTime.Now,
                TipoClase = "Teoría",
                Asistencias = new List<AsistenciaEstudianteDto>
                {
                    new AsistenciaEstudianteDto { IdEstudiante = 1, Presente = true },
                    new AsistenciaEstudianteDto { IdEstudiante = 2, Presente = false }
                }
            };

            var asistenciasDto = new List<AsistenciaDto>
            {
                new AsistenciaDto { Id = 1, IdEstudiante = 1, Presente = true },
                new AsistenciaDto { Id = 2, IdEstudiante = 2, Presente = false }
            };

            _mockAsistenciaService.Setup(s => s.RegistrarAsistenciasMasivasAsync(dto))
                .ReturnsAsync(asistenciasDto);

            // Act
            var result = await _controller.RegistrarAsistenciasMasivas(dto);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ActualizarAsistencia_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var dto = new ActualizarAsistenciaDto
            {
                Presente = false,
                Observaciones = "Llegó tarde"
            };

            var asistenciaActualizada = new AsistenciaDto
            {
                Id = 1,
                Presente = false,
                Observaciones = "Llegó tarde"
            };

            _mockAsistenciaService.Setup(s => s.ActualizarAsistenciaAsync(1, dto))
                .ReturnsAsync(asistenciaActualizada);

            // Act
            var result = await _controller.ActualizarAsistencia(1, dto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var asistencia = okResult.Value.Should().BeOfType<AsistenciaDto>().Subject;
            asistencia.Presente.Should().BeFalse();
        }

        [Fact]
        public async Task ActualizarAsistencia_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();

            var dto = new ActualizarAsistenciaDto { Presente = true };

            _mockAsistenciaService.Setup(s => s.ActualizarAsistenciaAsync(999, dto))
                .ThrowsAsync(new ArgumentException("Asistencia no encontrada"));

            // Act
            var result = await _controller.ActualizarAsistencia(999, dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task EliminarAsistencia_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            _mockAsistenciaService.Setup(s => s.EliminarAsistenciaAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.EliminarAsistencia(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task EliminarAsistencia_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();

            _mockAsistenciaService.Setup(s => s.EliminarAsistenciaAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.EliminarAsistencia(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetResumenAsistenciaCurso_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var resumen = new ResumenAsistenciaCursoDto
            {
                IdCurso = 1,
                NombreCurso = "Matemáticas",
                TotalEstudiantes = 30,
                TotalClases = 15,
                PorcentajeAsistenciaPromedio = 85.5m
            };

            _mockAsistenciaService.Setup(s => s.GetResumenAsistenciaCursoAsync(1, null, null))
                .ReturnsAsync(resumen);

            // Act
            var result = await _controller.GetResumenAsistenciaCurso(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var resumenResult = okResult.Value.Should().BeOfType<ResumenAsistenciaCursoDto>().Subject;
            resumenResult.TotalEstudiantes.Should().Be(30);
        }

        [Fact]
        public async Task GetAsistenciasPorCursoYFecha_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();
            var fecha = DateTime.Today;

            var asistencias = new List<AsistenciaDto>
            {
                new AsistenciaDto { Id = 1, IdCurso = 1, Fecha = fecha, Presente = true }
            };

            _mockAsistenciaService.Setup(s => s.GetAsistenciasPorCursoYFechaAsync(1, fecha))
                .ReturnsAsync(asistencias);

            // Act
            var result = await _controller.GetAsistenciasPorCursoYFecha(1, fecha);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GenerarReporteAsistencia_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();

            var reporte = new ReporteAsistenciaDto
            {
                NombreCurso = "Matemáticas",
                TotalEstudiantes = 30,
                TotalClases = 15
            };

            _mockAsistenciaService.Setup(s => s.GenerarReporteAsistenciaAsync(1, null, null))
                .ReturnsAsync(reporte);

            // Act
            var result = await _controller.GenerarReporteAsistencia(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }
    }
}
