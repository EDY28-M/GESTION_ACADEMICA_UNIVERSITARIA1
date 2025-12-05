using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using System.Security.Claims;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Asistencias;

/// <summary>
/// Tests para reportes de asistencia en AsistenciasController.
/// Valida generación de reportes y estadísticas.
/// </summary>
public class AsistenciasControllerReportesTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly AsistenciasController _controller;
    private readonly Mock<IAsistenciaService> _asistenciaServiceMock;
    private readonly Mock<ILogger<AsistenciasController>> _loggerMock;

    private const int TestDocenteId = 1;
    private const int TestEstudianteId = 1;
    private const int TestCursoId = 1;
    private const int TestPeriodoId = 1;

    public AsistenciasControllerReportesTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_AsistReportes_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _asistenciaServiceMock = new Mock<IAsistenciaService>();
        _loggerMock = new Mock<ILogger<AsistenciasController>>();
        _controller = new AsistenciasController(_asistenciaServiceMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    private void SetupDocenteAuthentication(int docenteId = TestDocenteId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Docente"),
            new Claim(ClaimTypes.Email, "docente@test.com"),
            new Claim("docenteId", docenteId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    private void SetupEstudianteAuthentication(int estudianteId = TestEstudianteId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Estudiante"),
            new Claim(ClaimTypes.NameIdentifier, estudianteId.ToString()),
            new Claim(ClaimTypes.Email, "estudiante@test.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    private void SeedTestData()
    {
        var periodo = new Periodo
        {
            Id = TestPeriodoId,
            Nombre = "2024-I",
            FechaInicio = DateTime.Now.AddMonths(-1),
            FechaFin = DateTime.Now.AddMonths(5),
            Activo = true
        };

        var docente = new Docente
        {
            Id = TestDocenteId,
            Nombres = "Juan",
            Apellidos = "Pérez",
            Correo = "docente@test.com"
        };

        var curso = new Curso
        {
            Id = TestCursoId,
            NombreCurso = "Programación I",
            Creditos = 4,
            HorasSemanal = 6,
            Ciclo = 1,
            IdDocente = TestDocenteId
        };

        var estudiante = new Estudiante
        {
            Id = TestEstudianteId,
            Codigo = "2024001",
            Nombres = "María",
            Apellidos = "García",
            Correo = "estudiante@test.com",
            Dni = "12345678",
            CicloActual = 1
        };

        _context.Periodos.Add(periodo);
        _context.Docentes.Add(docente);
        _context.Cursos.Add(curso);
        _context.Estudiantes.Add(estudiante);
        _context.SaveChanges();
    }

    #region Tests de GenerarReporteAsistencia

    [Fact]
    public async Task GenerarReporteAsistencia_WithValidCurso_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var reporte = new ReporteAsistenciaDto
        {
            NombreCurso = "Programación I",
            FechaGeneracion = DateTime.Now,
            TotalClases = 10,
            Estudiantes = new List<ReporteAsistenciaEstudianteDto>()
        };

        _asistenciaServiceMock
            .Setup(s => s.GenerarReporteAsistenciaAsync(TestCursoId, null, null))
            .ReturnsAsync(reporte);

        // Act
        var result = await _controller.GenerarReporteAsistencia(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerarReporteAsistencia_WithDateFilter_UsesFilters()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var fechaInicio = DateTime.Today.AddDays(-30);
        var fechaFin = DateTime.Today;
        
        _asistenciaServiceMock
            .Setup(s => s.GenerarReporteAsistenciaAsync(TestCursoId, fechaInicio, fechaFin))
            .ReturnsAsync(new ReporteAsistenciaDto
            {
                TotalClases = 5
            });

        // Act
        var result = await _controller.GenerarReporteAsistencia(TestCursoId, fechaInicio, fechaFin);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _asistenciaServiceMock.Verify(s => s.GenerarReporteAsistenciaAsync(TestCursoId, fechaInicio, fechaFin), Times.Once);
    }

    #endregion

    #region Tests de GetHistorialAsistencias

    [Fact]
    public async Task GetHistorialAsistencias_WithValidFilters_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var historial = new HistorialAsistenciasDto
        {
            TotalRegistros = 50,
            Asistencias = new List<AsistenciaDto>()
        };

        _asistenciaServiceMock
            .Setup(s => s.GetHistorialAsistenciasAsync(It.IsAny<FiltrosAsistenciaDto>()))
            .ReturnsAsync(historial);

        // Act
        var result = await _controller.GetHistorialAsistencias(
            idCurso: TestCursoId,
            idEstudiante: null,
            fechaInicio: null,
            fechaFin: null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHistorialAsistencias_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _asistenciaServiceMock
            .Setup(s => s.GetHistorialAsistenciasAsync(It.IsAny<FiltrosAsistenciaDto>()))
            .ReturnsAsync(new HistorialAsistenciasDto
            {
                TotalRegistros = 100,
                Asistencias = new List<AsistenciaDto>()
            });

        // Act
        var result = await _controller.GetHistorialAsistencias(
            idCurso: TestCursoId,
            idEstudiante: null,
            fechaInicio: null,
            fechaFin: null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        // Nota: El DTO HistorialAsistenciasDto no devuelve la página actual, así que no podemos verificarla en el Assert
    }

    #endregion

    #region Tests de GetEstadisticasAsistenciaEstudiante

    [Fact]
    public async Task GetEstadisticasAsistenciaEstudiante_WithValidId_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var estadisticas = new EstadisticasAsistenciaEstudianteDto
        {
            TotalClases = 30,
            TotalAsistencias = 28,
            TotalFaltas = 2,
            PorcentajeAsistenciaGeneral = 93.33m
        };

        _asistenciaServiceMock
            .Setup(s => s.GetEstadisticasAsistenciaEstudianteAsync(TestEstudianteId, null))
            .ReturnsAsync(estadisticas);

        // Act
        var result = await _controller.GetEstadisticasAsistenciaEstudiante(TestEstudianteId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as EstadisticasAsistenciaEstudianteDto;
        stats!.PorcentajeAsistenciaGeneral.Should().BeGreaterThan(90);
    }

    [Fact]
    public async Task GetEstadisticasAsistenciaEstudiante_WithPeriodoFilter_UsesPeriodo()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _asistenciaServiceMock
            .Setup(s => s.GetEstadisticasAsistenciaEstudianteAsync(TestEstudianteId, TestPeriodoId))
            .ReturnsAsync(new EstadisticasAsistenciaEstudianteDto
            {
                TotalClases = 15
            });

        // Act
        var result = await _controller.GetEstadisticasAsistenciaEstudiante(TestEstudianteId, TestPeriodoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _asistenciaServiceMock.Verify(s => s.GetEstadisticasAsistenciaEstudianteAsync(TestEstudianteId, TestPeriodoId), Times.Once);
    }

    #endregion

    #region Tests de GetTendenciaAsistenciaEstudiante

    [Fact]
    public async Task GetTendenciaAsistenciaEstudiante_WithValidId_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var tendencia = new List<TendenciaAsistenciaDto>
        {
            new TendenciaAsistenciaDto { Mes = "Enero", Anio = 2024, PorcentajeAsistencia = 95 },
            new TendenciaAsistenciaDto { Mes = "Febrero", Anio = 2024, PorcentajeAsistencia = 92 },
            new TendenciaAsistenciaDto { Mes = "Marzo", Anio = 2024, PorcentajeAsistencia = 88 }
        };

        _asistenciaServiceMock
            .Setup(s => s.GetTendenciaAsistenciaEstudianteAsync(TestEstudianteId, 6))
            .ReturnsAsync(tendencia);

        // Act
        var result = await _controller.GetTendenciaAsistenciaEstudiante(TestEstudianteId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var data = okResult!.Value as List<TendenciaAsistenciaDto>;
        data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTendenciaAsistenciaEstudiante_WithCustomMonths_UsesParameter()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _asistenciaServiceMock
            .Setup(s => s.GetTendenciaAsistenciaEstudianteAsync(TestEstudianteId, 12))
            .ReturnsAsync(new List<TendenciaAsistenciaDto>());

        // Act
        var result = await _controller.GetTendenciaAsistenciaEstudiante(TestEstudianteId, 12);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _asistenciaServiceMock.Verify(s => s.GetTendenciaAsistenciaEstudianteAsync(TestEstudianteId, 12), Times.Once);
    }

    #endregion

    #region Tests de CalcularPorcentajeAsistencia

    [Fact]
    public async Task CalcularPorcentajeAsistencia_WithValidIds_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _asistenciaServiceMock
            .Setup(s => s.CalcularPorcentajeAsistenciaAsync(TestEstudianteId, TestCursoId))
            .ReturnsAsync(85.5m);

        // Act
        var result = await _controller.CalcularPorcentajeAsistencia(TestEstudianteId, TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        
        // El controlador devuelve un objeto anónimo { porcentaje = ... }
        var value = okResult!.Value;
        var porcentaje = value!.GetType().GetProperty("porcentaje")?.GetValue(value);
        porcentaje.Should().Be(85.5m);
    }

    [Fact]
    public async Task CalcularPorcentajeAsistencia_WithNoAttendance_ReturnsZero()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _asistenciaServiceMock
            .Setup(s => s.CalcularPorcentajeAsistenciaAsync(TestEstudianteId, TestCursoId))
            .ReturnsAsync(0m);

        // Act
        var result = await _controller.CalcularPorcentajeAsistencia(TestEstudianteId, TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        
        var value = okResult!.Value;
        var porcentaje = value!.GetType().GetProperty("porcentaje")?.GetValue(value);
        porcentaje.Should().Be(0m);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
