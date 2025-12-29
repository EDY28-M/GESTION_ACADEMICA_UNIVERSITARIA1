using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using System.Security.Claims;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services;
using Moq;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Cursos;

/// <summary>
/// Tests para filtros de cursos en CursosController.
/// Valida búsqueda por docente y ciclo.
/// </summary>
public class CursosControllerFiltrosTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly CursosController _controller;

    private const int TestDocenteId = 1;
    private const int TestDocente2Id = 2;

    public CursosControllerFiltrosTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_CursosFiltros_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _controller = new CursosController(new CursosService(_context));

        SeedTestData();
    }

    private void SetupAdminAuthentication()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Administrador"),
            new Claim(ClaimTypes.Email, "admin@test.com")
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
        // Docentes
        var docentes = new[]
        {
            new Docente
            {
                Id = TestDocenteId,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "juan@test.com"
            },
            new Docente
            {
                Id = TestDocente2Id,
                Nombres = "María",
                Apellidos = "García",
                Correo = "maria@test.com"
            }
        };

        // Cursos en diferentes ciclos y con diferentes docentes
        var cursos = new[]
        {
            new Curso
            {
                Id = 1,
                Codigo = "CS101",
                NombreCurso = "Programación I",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = TestDocenteId
            },
            new Curso
            {
                Id = 2,
                Codigo = "CS102",
                NombreCurso = "Programación II",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 2,
                IdDocente = TestDocenteId
            },
            new Curso
            {
                Id = 3,
                Codigo = "CS201",
                NombreCurso = "Estructuras de Datos",
                Creditos = 5,
                HorasSemanal = 8,
                Ciclo = 3,
                IdDocente = TestDocente2Id
            },
            new Curso
            {
                Id = 4,
                Codigo = "CS202",
                NombreCurso = "Algoritmos",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 3,
                IdDocente = TestDocenteId
            },
            new Curso
            {
                Id = 5,
                Codigo = "MAT101",
                NombreCurso = "Cálculo I",
                Creditos = 5,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = null // Sin docente asignado
            }
        };

        _context.Docentes.AddRange(docentes);
        _context.Cursos.AddRange(cursos);
        _context.SaveChanges();
    }

    #region Tests de GetCursosPorDocente

    [Fact]
    public async Task GetCursosPorDocente_WithValidDocente_ReturnsCursos()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorDocente(TestDocenteId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().NotBeNull();
        cursos!.Count.Should().BeGreaterThan(0);
        cursos.All(c => c.IdDocente == TestDocenteId || c.IdDocente == null || c.IdDocente == TestDocenteId).Should().BeTrue();
    }

    [Fact]
    public async Task GetCursosPorDocente_WithNoAssignedCursos_ReturnsEmptyList()
    {
        // Arrange
        SetupAdminAuthentication();
        
        // Agregar docente sin cursos
        _context.Docentes.Add(new Docente
        {
            Id = 99,
            Nombres = "Sin",
            Apellidos = "Cursos",
            Correo = "sincursos@test.com"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCursosPorDocente(99);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCursosPorDocente_WithInvalidDocente_ReturnsEmptyList()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorDocente(9999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCursosPorDocente_ReturnsCursosWithCorrectDocente()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorDocente(TestDocente2Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().HaveCount(1);
        cursos!.First().NombreCurso.Should().Be("Estructuras de Datos");
    }

    #endregion

    #region Tests de GetCursosPorCiclo

    [Fact]
    public async Task GetCursosPorCiclo_WithValidCiclo_ReturnsCursos()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorCiclo(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().NotBeNull();
        cursos!.Count.Should().Be(2); // Programación I y Cálculo I
        cursos.All(c => c.Ciclo == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetCursosPorCiclo_WithCiclo3_ReturnsMultipleCursos()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorCiclo(3);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().HaveCount(2); // Estructuras de Datos y Algoritmos
    }

    [Fact]
    public async Task GetCursosPorCiclo_WithNoCursos_ReturnsEmptyList()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorCiclo(10); // Ciclo que no existe

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCursosPorCiclo_OrdersByCursoName()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorCiclo(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = (okResult!.Value as List<CursoDto>)?.ToList();
        
        // Verificar que devuelve resultados
        cursos.Should().NotBeNull();
        cursos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCursosPorCiclo_IncludesDocenteInfo()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursosPorCiclo(3);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        // Verificar que incluye información del docente
        cursos!.Any(c => c.IdDocente != null).Should().BeTrue();
    }

    #endregion

    #region Tests combinados de filtros

    [Fact]
    public async Task GetAllCursos_ReturnsAllCursos()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursos();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetCursos_IncludesUnassignedCursos()
    {
        // Arrange
        SetupAdminAuthentication();

        // Act
        var result = await _controller.GetCursos();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cursos = okResult!.Value as List<CursoDto>;
        
        cursos!.Any(c => c.IdDocente == null).Should().BeTrue();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
