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

        #region GetEstadisticasCompletasAsistencia Tests

        [Fact]
        public async Task GetEstadisticasCompletasAsistencia_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAuthenticatedUser();

            var estadisticas = new EstadisticasAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                TotalSesionesEsperadas = 20,
                TotalAsistencias = 18,
                AsistenciasPresente = 18,
                AsistenciasFalta = 2,
                PorcentajeAsistencia = 90m,
                PorcentajeInasistencia = 10m,
                PuedeDarExamenFinal = true,
                MensajeBloqueo = string.Empty
            };

            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.GetEstadisticasCompletasAsistencia(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedStats = okResult.Value.Should().BeOfType<EstadisticasAsistenciaDto>().Subject;
            returnedStats.PorcentajeAsistencia.Should().Be(90m);
        }

        [Fact]
        public async Task GetEstadisticasCompletasAsistencia_WithInvalidStudent_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new ArgumentException("Estudiante no encontrado"));

            // Act
            var result = await _controller.GetEstadisticasCompletasAsistencia(999, 1);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEstadisticasCompletasAsistencia_WithInvalidCurso_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new ArgumentException("Curso no encontrado"));

            // Act
            var result = await _controller.GetEstadisticasCompletasAsistencia(1, 999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEstadisticasCompletasAsistencia_WithOver30PercentAbsences_ReturnsPuedeDarFalse()
        {
            // Arrange
            SetupAuthenticatedUser();

            var estadisticas = new EstadisticasAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                TotalSesionesEsperadas = 20,
                TotalAsistencias = 12,
                AsistenciasPresente = 12,
                AsistenciasFalta = 8,
                PorcentajeAsistencia = 60m,
                PorcentajeInasistencia = 40m,
                PuedeDarExamenFinal = false,
                MensajeBloqueo = "Ha superado el 30% de inasistencias. No podrá rendir el examen final."
            };

            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.GetEstadisticasCompletasAsistencia(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedStats = okResult.Value.Should().BeOfType<EstadisticasAsistenciaDto>().Subject;
            returnedStats.PuedeDarExamenFinal.Should().BeFalse();
            returnedStats.MensajeBloqueo.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region PuedeRendirExamenFinal Tests

        [Fact]
        public async Task PuedeRendirExamenFinal_WhenCanTakeExam_ReturnsTrue()
        {
            // Arrange
            SetupAuthenticatedUser();

            var estadisticas = new EstadisticasAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                TotalSesionesEsperadas = 20,
                TotalAsistencias = 18,
                AsistenciasPresente = 18,
                AsistenciasFalta = 2,
                PorcentajeAsistencia = 90m,
                PorcentajeInasistencia = 10m,
                PuedeDarExamenFinal = true,
                MensajeBloqueo = string.Empty
            };

            _mockAsistenciaService.Setup(s => s.PuedeDarExamenFinalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(true);
            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.PuedeRendirExamenFinal(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task PuedeRendirExamenFinal_WhenCannotTakeExam_ReturnsFalse()
        {
            // Arrange
            SetupAuthenticatedUser();

            var estadisticas = new EstadisticasAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                TotalSesionesEsperadas = 20,
                TotalAsistencias = 12,
                AsistenciasPresente = 12,
                AsistenciasFalta = 8,
                PorcentajeAsistencia = 60m,
                PorcentajeInasistencia = 40m,
                PuedeDarExamenFinal = false,
                MensajeBloqueo = "Ha superado el 30% de inasistencias"
            };

            _mockAsistenciaService.Setup(s => s.PuedeDarExamenFinalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(false);
            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.PuedeRendirExamenFinal(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task PuedeRendirExamenFinal_WithInvalidStudent_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            _mockAsistenciaService.Setup(s => s.PuedeDarExamenFinalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new ArgumentException("Estudiante no encontrado"));

            // Act
            var result = await _controller.PuedeRendirExamenFinal(999, 1);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task PuedeRendirExamenFinal_WithInvalidCurso_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            _mockAsistenciaService.Setup(s => s.PuedeDarExamenFinalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new ArgumentException("Curso no encontrado"));

            // Act
            var result = await _controller.PuedeRendirExamenFinal(1, 999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task PuedeRendirExamenFinal_AtExactly30Percent_ReturnsFalse()
        {
            // Arrange
            SetupAuthenticatedUser();

            var estadisticas = new EstadisticasAsistenciaDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                TotalSesionesEsperadas = 20,
                TotalAsistencias = 14,
                AsistenciasPresente = 14,
                AsistenciasFalta = 6,
                PorcentajeAsistencia = 70m,
                PorcentajeInasistencia = 30m,
                PuedeDarExamenFinal = false,
                MensajeBloqueo = "Ha alcanzado el límite del 30% de inasistencias"
            };

            _mockAsistenciaService.Setup(s => s.PuedeDarExamenFinalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(false);
            _mockAsistenciaService.Setup(s => s.CalcularEstadisticasAsistenciaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.PuedeRendirExamenFinal(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        #endregion
    }
}
