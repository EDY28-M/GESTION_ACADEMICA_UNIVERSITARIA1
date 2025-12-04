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
    public class AsistenciasControllerAuxiliaresTests
    {
        private readonly Mock<IAsistenciaService> _mockAsistenciaService;
        private readonly Mock<ILogger<AsistenciasController>> _mockLogger;
        private readonly AsistenciasController _controller;

        public AsistenciasControllerAuxiliaresTests()
        {
            _mockAsistenciaService = new Mock<IAsistenciaService>();
            _mockLogger = new Mock<ILogger<AsistenciasController>>();
            _controller = new AsistenciasController(_mockAsistenciaService.Object, _mockLogger.Object);
        }

        private void SetupAuthenticatedUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
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

        [Fact]
        public async Task ExisteAsistencia_WhenExists_ReturnsTrue()
        {
            // Arrange
            SetupAuthenticatedUser();
            var fecha = DateTime.Today;

            _mockAsistenciaService.Setup(s => s.ExisteAsistenciaAsync(1, 1, fecha))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ExisteAsistencia(1, 1, fecha);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task ExisteAsistencia_WhenNotExists_ReturnsFalse()
        {
            // Arrange
            SetupAuthenticatedUser();
            var fecha = DateTime.Today;

            _mockAsistenciaService.Setup(s => s.ExisteAsistenciaAsync(1, 1, fecha))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ExisteAsistencia(1, 1, fecha);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task CalcularPorcentajeAsistencia_ReturnsOk()
        {
            // Arrange
            SetupAuthenticatedUser();

            _mockAsistenciaService.Setup(s => s.CalcularPorcentajeAsistenciaAsync(1, 1))
                .ReturnsAsync(85.5m);

            // Act
            var result = await _controller.CalcularPorcentajeAsistencia(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetHistorialAsistencias_WithFilters_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var historial = new HistorialAsistenciasDto
            {
                TotalRegistros = 50,
                TotalAsistencias = 40,
                TotalFaltas = 10,
                PorcentajeAsistencia = 80m,
                Asistencias = new List<AsistenciaDto>()
            };

            _mockAsistenciaService.Setup(s => s.GetHistorialAsistenciasAsync(It.IsAny<FiltrosAsistenciaDto>()))
                .ReturnsAsync(historial);

            // Act
            var result = await _controller.GetHistorialAsistencias(
                idEstudiante: 1,
                idCurso: 1,
                fechaInicio: DateTime.Today.AddMonths(-1),
                fechaFin: DateTime.Today);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetHistorialAsistencias_WithoutFilters_ReturnsOk()
        {
            // Arrange
            SetupDocenteUser();

            var historial = new HistorialAsistenciasDto
            {
                TotalRegistros = 100,
                TotalAsistencias = 80,
                TotalFaltas = 20,
                PorcentajeAsistencia = 80m,
                Asistencias = new List<AsistenciaDto>()
            };

            _mockAsistenciaService.Setup(s => s.GetHistorialAsistenciasAsync(It.IsAny<FiltrosAsistenciaDto>()))
                .ReturnsAsync(historial);

            // Act
            var result = await _controller.GetHistorialAsistencias();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }
    }
}
