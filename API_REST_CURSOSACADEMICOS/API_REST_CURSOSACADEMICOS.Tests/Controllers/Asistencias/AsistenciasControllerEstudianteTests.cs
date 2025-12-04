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
    public class AsistenciasControllerEstudianteTests
    {
        private readonly Mock<IAsistenciaService> _mockAsistenciaService;
        private readonly Mock<ILogger<AsistenciasController>> _mockLogger;
        private readonly AsistenciasController _controller;

        public AsistenciasControllerEstudianteTests()
        {
            _mockAsistenciaService = new Mock<IAsistenciaService>();
            _mockLogger = new Mock<ILogger<AsistenciasController>>();
            _controller = new AsistenciasController(_mockAsistenciaService.Object, _mockLogger.Object);
        }

        private void SetupEstudianteUser(int userId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
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
        public async Task GetAsistenciasEstudiante_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupEstudianteUser();

            var asistencias = new List<AsistenciasPorCursoDto>
            {
                new AsistenciasPorCursoDto
                {
                    IdCurso = 1,
                    NombreCurso = "Matemáticas",
                    TotalClases = 15,
                    TotalAsistencias = 12,
                    TotalFaltas = 3,
                    PorcentajeAsistencia = 80m
                }
            };

            _mockAsistenciaService.Setup(s => s.GetAsistenciasPorEstudianteAsync(1, null))
                .ReturnsAsync(asistencias);

            // Act
            var result = await _controller.GetAsistenciasEstudiante(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetResumenAsistenciaEstudianteCurso_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupEstudianteUser();

            var resumen = new ResumenAsistenciaEstudianteDto
            {
                IdEstudiante = 1,
                IdCurso = 1,
                NombreCurso = "Matemáticas",
                TotalClases = 15,
                TotalAsistencias = 12,
                PorcentajeAsistencia = 80m
            };

            _mockAsistenciaService.Setup(s => s.GetResumenAsistenciaEstudianteCursoAsync(1, 1))
                .ReturnsAsync(resumen);

            // Act
            var result = await _controller.GetResumenAsistenciaEstudianteCurso(1, 1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetResumenAsistenciaEstudianteCurso_WithInvalidData_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser();

            _mockAsistenciaService.Setup(s => s.GetResumenAsistenciaEstudianteCursoAsync(999, 999))
                .ThrowsAsync(new ArgumentException("Estudiante o curso no encontrado"));

            // Act
            var result = await _controller.GetResumenAsistenciaEstudianteCurso(999, 999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEstadisticasAsistenciaEstudiante_ReturnsOk()
        {
            // Arrange
            SetupEstudianteUser();

            var estadisticas = new EstadisticasAsistenciaEstudianteDto
            {
                TotalCursos = 5,
                TotalClases = 75,
                TotalAsistencias = 60,
                TotalFaltas = 15,
                PorcentajeAsistenciaGeneral = 80m,
                CursosConAlerta = 1
            };

            _mockAsistenciaService.Setup(s => s.GetEstadisticasAsistenciaEstudianteAsync(1, null))
                .ReturnsAsync(estadisticas);

            // Act
            var result = await _controller.GetEstadisticasAsistenciaEstudiante(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetTendenciaAsistenciaEstudiante_ReturnsOk()
        {
            // Arrange
            SetupEstudianteUser();

            var tendencia = new List<TendenciaAsistenciaDto>
            {
                new TendenciaAsistenciaDto
                {
                    Mes = "Enero 2024",
                    Anio = 2024,
                    NumeroMes = 1,
                    TotalClases = 20,
                    TotalAsistencias = 18,
                    PorcentajeAsistencia = 90m
                },
                new TendenciaAsistenciaDto
                {
                    Mes = "Febrero 2024",
                    Anio = 2024,
                    NumeroMes = 2,
                    TotalClases = 18,
                    TotalAsistencias = 15,
                    PorcentajeAsistencia = 83.33m
                }
            };

            _mockAsistenciaService.Setup(s => s.GetTendenciaAsistenciaEstudianteAsync(1, 6))
                .ReturnsAsync(tendencia);

            // Act
            var result = await _controller.GetTendenciaAsistenciaEstudiante(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public void GetMisAsistencias_WithEstudianteRole_ReturnsBadRequest()
        {
            // Arrange
            SetupEstudianteUser();

            // Act
            var result = _controller.GetMisAsistencias();

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GetMisAsistencias_WithDocenteRole_ReturnsForbid()
        {
            // Arrange
            SetupDocenteUser();

            // Act
            var result = _controller.GetMisAsistencias();

            // Assert
            result.Result.Should().BeOfType<ForbidResult>();
        }
    }
}
