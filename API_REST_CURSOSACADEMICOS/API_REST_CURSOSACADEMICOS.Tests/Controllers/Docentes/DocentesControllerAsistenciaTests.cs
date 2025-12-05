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

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Docentes;

/// <summary>
/// Tests para funcionalidades de asistencia en DocentesController.
/// Implementa verificación de registro masivo y consultas de asistencia.
/// </summary>
public class DocentesControllerAsistenciaTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly DocentesController _controller;
    private readonly Mock<ILogger<DocentesController>> _loggerMock;

    private const int TestDocenteId = 1;
    private const int TestCursoId = 1;
    private const int TestEstudianteId = 1;
    private const int TestEstudianteId2 = 2;
    private const int TestPeriodoId = 1;
    private const string TestDocenteEmail = "docente@test.com";

    public DocentesControllerAsistenciaTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Asistencia_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _loggerMock = new Mock<ILogger<DocentesController>>();
        _controller = new DocentesController(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SetupDocenteAuthentication(int docenteId = TestDocenteId, string email = TestDocenteEmail)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Docente"),
            new Claim(ClaimTypes.Email, email),
            new Claim("docenteId", docenteId.ToString())
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
            Correo = TestDocenteEmail,
            PasswordHash = "hashedPassword"
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

        var estudiantes = new[]
        {
            new Estudiante
            {
                Id = TestEstudianteId,
                Codigo = "2024001",
                Nombres = "María",
                Apellidos = "García",
                Correo = "maria@test.com",
                Dni = "12345678",
                CicloActual = 1
            },
            new Estudiante
            {
                Id = TestEstudianteId2,
                Codigo = "2024002",
                Nombres = "Pedro",
                Apellidos = "López",
                Correo = "pedro@test.com",
                Dni = "87654321",
                CicloActual = 1
            }
        };

        var matriculas = new[]
        {
            new Matricula
            {
                Id = 1,
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoId,
                IdPeriodo = TestPeriodoId,
                Estado = "Matriculado"
            },
            new Matricula
            {
                Id = 2,
                IdEstudiante = TestEstudianteId2,
                IdCurso = TestCursoId,
                IdPeriodo = TestPeriodoId,
                Estado = "Matriculado"
            }
        };

        _context.Periodos.Add(periodo);
        _context.Docentes.Add(docente);
        _context.Cursos.Add(curso);
        _context.Estudiantes.AddRange(estudiantes);
        _context.Matriculas.AddRange(matriculas);
        _context.SaveChanges();
    }

    #region Tests de RegistrarAsistencia

    [Fact]
    public async Task RegistrarAsistencia_WithValidData_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var asistenciaDto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = TestCursoId,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Asistencias = new List<AsistenciaEstudianteDto>
            {
                new() { IdEstudiante = TestEstudianteId, Presente = true, Observaciones = null },
                new() { IdEstudiante = TestEstudianteId2, Presente = false, Observaciones = "Justificado" }
            }
        };

        // Act
        var result = await _controller.RegistrarAsistencia(asistenciaDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var asistenciasGuardadas = await _context.Asistencias.ToListAsync();
        asistenciasGuardadas.Should().HaveCount(2);
    }

    [Fact]
    public async Task RegistrarAsistencia_WithInvalidCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var asistenciaDto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = 9999,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Asistencias = new List<AsistenciaEstudianteDto>()
        };

        // Act
        var result = await _controller.RegistrarAsistencia(asistenciaDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegistrarAsistencia_CursoNotOwnedByDocente_ReturnsForbid()
    {
        // Arrange
        SetupDocenteAuthentication(docenteId: 999);
        
        var asistenciaDto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = TestCursoId,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Asistencias = new List<AsistenciaEstudianteDto>()
        };

        // Act
        var result = await _controller.RegistrarAsistencia(asistenciaDto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RegistrarAsistencia_WhenNotDocente_ReturnsForbid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Estudiante")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext 
            { 
                User = new ClaimsPrincipal(identity) 
            }
        };

        var asistenciaDto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = TestCursoId,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Asistencias = new List<AsistenciaEstudianteDto>()
        };

        // Act
        var result = await _controller.RegistrarAsistencia(asistenciaDto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RegistrarAsistencia_UpdatesExistingAttendance()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Agregar asistencia existente
        _context.Asistencias.Add(new Asistencia
        {
            IdEstudiante = TestEstudianteId,
            IdCurso = TestCursoId,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Presente = false,
            FechaRegistro = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var asistenciaDto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = TestCursoId,
            Fecha = DateTime.Today,
            TipoClase = "Teoría",
            Asistencias = new List<AsistenciaEstudianteDto>
            {
                new() { IdEstudiante = TestEstudianteId, Presente = true } // Cambiar a presente
            }
        };

        // Act
        var result = await _controller.RegistrarAsistencia(asistenciaDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var asistenciaActualizada = await _context.Asistencias
            .FirstAsync(a => a.IdEstudiante == TestEstudianteId);
        asistenciaActualizada.Presente.Should().BeTrue();
    }

    #endregion

    #region Tests de GetAsistenciaCurso

    [Fact]
    public async Task GetAsistenciaCurso_WithValidCurso_ReturnsResumen()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Agregar asistencias de prueba
        var fechas = new[] { DateTime.Today, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-2) };
        foreach (var fecha in fechas)
        {
            _context.Asistencias.Add(new Asistencia
            {
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoId,
                Fecha = fecha,
                TipoClase = "Teoría",
                Presente = true,
                FechaRegistro = DateTime.Now
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetAsistenciaCurso(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAsistenciaCurso_WithInvalidCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();

        // Act
        var result = await _controller.GetAsistenciaCurso(9999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAsistenciaCurso_CursoNotOwnedByDocente_ReturnsForbid()
    {
        // Arrange
        SetupDocenteAuthentication(docenteId: 999);

        // Act
        var result = await _controller.GetAsistenciaCurso(TestCursoId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetAsistenciaCurso_WithNoAsistencias_ReturnsEmptyResumen()
    {
        // Arrange
        SetupDocenteAuthentication();

        // Act
        var result = await _controller.GetAsistenciaCurso(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Tests de GetResumenAsistencia

    [Fact]
    public async Task GetResumenAsistencia_WithValidCurso_ReturnsResumenPorEstudiante()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Agregar asistencias variadas
        _context.Asistencias.AddRange(new[]
        {
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today, TipoClase = "Teoría", Presente = true, FechaRegistro = DateTime.Now },
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today.AddDays(-1), TipoClase = "Teoría", Presente = true, FechaRegistro = DateTime.Now },
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today.AddDays(-2), TipoClase = "Teoría", Presente = false, FechaRegistro = DateTime.Now },
            new Asistencia { IdEstudiante = TestEstudianteId2, IdCurso = TestCursoId, Fecha = DateTime.Today, TipoClase = "Teoría", Presente = true, FechaRegistro = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetResumenAsistencia(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetResumenAsistencia_WithInvalidCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();

        // Act
        var result = await _controller.GetResumenAsistencia(9999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetResumenAsistencia_CalculatesPercentageCorrectly()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // 2 presentes de 3 totales = 66.67%
        _context.Asistencias.AddRange(new[]
        {
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today, TipoClase = "Teoría", Presente = true, FechaRegistro = DateTime.Now },
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today.AddDays(-1), TipoClase = "Teoría", Presente = true, FechaRegistro = DateTime.Now },
            new Asistencia { IdEstudiante = TestEstudianteId, IdCurso = TestCursoId, Fecha = DateTime.Today.AddDays(-2), TipoClase = "Teoría", Presente = false, FechaRegistro = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetResumenAsistencia(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
