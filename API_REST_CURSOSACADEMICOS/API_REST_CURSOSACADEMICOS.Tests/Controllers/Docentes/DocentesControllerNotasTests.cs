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
using System.Text.Json;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Docentes;

/// <summary>
/// Tests para funcionalidades de notas en DocentesController.
/// Sigue el patrón AAA (Arrange-Act-Assert) y buenas prácticas de testing.
/// </summary>
public class DocentesControllerNotasTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly DocentesController _controller;
    private readonly Mock<ILogger<DocentesController>> _loggerMock;

    // Constantes para datos de prueba - mejora mantenibilidad
    private const int TestDocenteId = 1;
    private const int TestCursoId = 1;
    private const int TestEstudianteId = 1;
    private const int TestMatriculaId = 1;
    private const int TestPeriodoId = 1;
    private const string TestDocenteEmail = "docente@test.com";

    public DocentesControllerNotasTests()
    {
        // Configurar base de datos en memoria con nombre único para aislamiento
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Notas_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _loggerMock = new Mock<ILogger<DocentesController>>();
        _controller = new DocentesController(_context, _loggerMock.Object);

        // Seed inicial de datos comunes
        SeedTestData();
    }

    /// <summary>
    /// Configura el contexto de usuario autenticado como docente
    /// </summary>
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

    /// <summary>
    /// Seed de datos de prueba para garantizar estado consistente
    /// </summary>
    private void SeedTestData()
    {
        // Periodo activo
        var periodo = new Periodo
        {
            Id = TestPeriodoId,
            Nombre = "2024-I",
            FechaInicio = DateTime.Now.AddMonths(-1),
            FechaFin = DateTime.Now.AddMonths(5),
            Activo = true
        };

        // Docente
        var docente = new Docente
        {
            Id = TestDocenteId,
            Nombres = "Juan",
            Apellidos = "Pérez",
            Correo = TestDocenteEmail,
            PasswordHash = "hashedPassword"
        };

        // Curso asignado al docente
        var curso = new Curso
        {
            Id = TestCursoId,
            NombreCurso = "Programación I",
            Creditos = 4,
            HorasSemanal = 6,
            Ciclo = 1,
            IdDocente = TestDocenteId
        };

        // Estudiante
        var estudiante = new Estudiante
        {
            Id = TestEstudianteId,
            Codigo = "2024001",
            Nombres = "María",
            Apellidos = "García",
            Correo = "maria@test.com",
            Dni = "12345678",
            CicloActual = 1
        };

        // Matrícula
        var matricula = new Matricula
        {
            Id = TestMatriculaId,
            IdEstudiante = TestEstudianteId,
            IdCurso = TestCursoId,
            IdPeriodo = TestPeriodoId,
            Estado = "Matriculado",
            FechaMatricula = DateTime.Now
        };

        _context.Periodos.Add(periodo);
        _context.Docentes.Add(docente);
        _context.Cursos.Add(curso);
        _context.Estudiantes.Add(estudiante);
        _context.Matriculas.Add(matricula);
        _context.SaveChanges();
    }

    #region Tests de RegistrarNotas

    [Fact]
    public async Task RegistrarNotas_WithValidData_ReturnsOkWithPromedioFinal()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = TestMatriculaId,
            parcial1 = 15,
            parcial2 = 16,
            practicas = 17,
            medioCurso = 14,
            examenFinal = 15,
            actitud = 18,
            trabajos = 16
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RegistrarNotas_WithMissingIdMatricula_ReturnsBadRequest()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var notasJson = JsonSerializer.SerializeToElement(new
        {
            parcial1 = 15,
            parcial2 = 16
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RegistrarNotas_WithInvalidMatricula_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = 9999, // ID inexistente
            parcial1 = 15
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegistrarNotas_WithCursoNotOwnedByDocente_ReturnsForbid()
    {
        // Arrange
        SetupDocenteAuthentication(docenteId: 999); // Docente diferente
        
        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = TestMatriculaId,
            parcial1 = 15
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RegistrarNotas_WhenNotDocente_ReturnsForbid()
    {
        // Arrange - Usuario sin rol docente
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Estudiante"),
            new Claim(ClaimTypes.Email, "estudiante@test.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext 
            { 
                User = new ClaimsPrincipal(identity) 
            }
        };

        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = TestMatriculaId,
            parcial1 = 15
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RegistrarNotas_WithInexistentCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = TestMatriculaId,
            parcial1 = 15
        });

        // Act
        var result = await _controller.RegistrarNotas(9999, notasJson); // Curso inexistente

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegistrarNotas_UpdatesExistingNotes_Successfully()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Agregar nota existente
        _context.Notas.Add(new Nota
        {
            IdMatricula = TestMatriculaId,
            TipoEvaluacion = "Parcial 1",
            NotaValor = 10,
            Peso = 10,
            Fecha = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var notasJson = JsonSerializer.SerializeToElement(new
        {
            idMatricula = TestMatriculaId,
            parcial1 = 18 // Nueva nota
        });

        // Act
        var result = await _controller.RegistrarNotas(TestCursoId, notasJson);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Tests de ObtenerTiposEvaluacion

    [Fact]
    public async Task ObtenerTiposEvaluacion_WithConfiguredTypes_ReturnsConfiguredTypes()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Agregar tipos de evaluación configurados
        _context.TiposEvaluacion.AddRange(new[]
        {
            new TipoEvaluacion { IdCurso = TestCursoId, Nombre = "Examen Parcial", Peso = 30, Orden = 1, Activo = true },
            new TipoEvaluacion { IdCurso = TestCursoId, Nombre = "Examen Final", Peso = 40, Orden = 2, Activo = true },
            new TipoEvaluacion { IdCurso = TestCursoId, Nombre = "Trabajos", Peso = 30, Orden = 3, Activo = true }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ObtenerTiposEvaluacion(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var tipos = okResult!.Value as List<TipoEvaluacionDto>;
        tipos.Should().HaveCount(3);
        tipos!.Sum(t => t.Peso).Should().Be(100);
    }

    [Fact]
    public async Task ObtenerTiposEvaluacion_WithNoConfiguredTypes_ReturnsDefaultTypes()
    {
        // Arrange
        SetupDocenteAuthentication();

        // Act
        var result = await _controller.ObtenerTiposEvaluacion(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var tipos = okResult!.Value as List<TipoEvaluacionDto>;
        tipos.Should().HaveCount(7); // Tipos por defecto
    }

    [Fact]
    public async Task ObtenerTiposEvaluacion_WithInvalidCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();

        // Act
        var result = await _controller.ObtenerTiposEvaluacion(9999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ObtenerTiposEvaluacion_CursoNotOwnedByDocente_ReturnsForbid()
    {
        // Arrange
        SetupDocenteAuthentication(docenteId: 999);

        // Act
        var result = await _controller.ObtenerTiposEvaluacion(TestCursoId);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ObtenerTiposEvaluacion_OnlyReturnsActiveTypes()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        _context.TiposEvaluacion.AddRange(new[]
        {
            new TipoEvaluacion { IdCurso = TestCursoId, Nombre = "Activo", Peso = 100, Orden = 1, Activo = true },
            new TipoEvaluacion { IdCurso = TestCursoId, Nombre = "Inactivo", Peso = 0, Orden = 2, Activo = false }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.ObtenerTiposEvaluacion(TestCursoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var tipos = okResult!.Value as List<TipoEvaluacionDto>;
        tipos.Should().HaveCount(1);
        tipos!.First().Nombre.Should().Be("Activo");
    }

    #endregion

    #region Tests de ConfigurarTiposEvaluacion

    [Fact]
    public async Task ConfigurarTiposEvaluacion_WithValidData_ReturnsOk()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var configDto = new ConfigurarTiposEvaluacionDto
        {
            TiposEvaluacion = new List<ActualizarTipoEvaluacionDto>
            {
                new() { Id = 0, Nombre = "Parcial 1", Peso = 25, Orden = 1, Activo = true },
                new() { Id = 0, Nombre = "Parcial 2", Peso = 25, Orden = 2, Activo = true },
                new() { Id = 0, Nombre = "Final", Peso = 50, Orden = 3, Activo = true }
            }
        };

        // Act
        var result = await _controller.ConfigurarTiposEvaluacion(TestCursoId, configDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verificar que se guardaron en la BD
        var tiposGuardados = await _context.TiposEvaluacion
            .Where(t => t.IdCurso == TestCursoId)
            .ToListAsync();
        tiposGuardados.Should().HaveCount(3);
    }

    [Fact]
    public async Task ConfigurarTiposEvaluacion_WithInvalidSum_ReturnsBadRequest()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var configDto = new ConfigurarTiposEvaluacionDto
        {
            TiposEvaluacion = new List<ActualizarTipoEvaluacionDto>
            {
                new() { Id = 0, Nombre = "Parcial", Peso = 50, Orden = 1, Activo = true },
                // Solo suma 50%, debería ser 100%
            }
        };

        // Act
        var result = await _controller.ConfigurarTiposEvaluacion(TestCursoId, configDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ConfigurarTiposEvaluacion_UpdatesExistingTypes()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        // Crear tipo existente
        var tipoExistente = new TipoEvaluacion
        {
            IdCurso = TestCursoId,
            Nombre = "Parcial Original",
            Peso = 100,
            Orden = 1,
            Activo = true
        };
        _context.TiposEvaluacion.Add(tipoExistente);
        await _context.SaveChangesAsync();

        var configDto = new ConfigurarTiposEvaluacionDto
        {
            TiposEvaluacion = new List<ActualizarTipoEvaluacionDto>
            {
                new() { Id = tipoExistente.Id, Nombre = "Parcial Modificado", Peso = 100, Orden = 1, Activo = true }
            }
        };

        // Act
        var result = await _controller.ConfigurarTiposEvaluacion(TestCursoId, configDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        var tipoActualizado = await _context.TiposEvaluacion.FindAsync(tipoExistente.Id);
        tipoActualizado!.Nombre.Should().Be("Parcial Modificado");
    }

    [Fact]
    public async Task ConfigurarTiposEvaluacion_WithInvalidCurso_ReturnsNotFound()
    {
        // Arrange
        SetupDocenteAuthentication();
        
        var configDto = new ConfigurarTiposEvaluacionDto
        {
            TiposEvaluacion = new List<ActualizarTipoEvaluacionDto>
            {
                new() { Id = 0, Nombre = "Test", Peso = 100, Orden = 1, Activo = true }
            }
        };

        // Act
        var result = await _controller.ConfigurarTiposEvaluacion(9999, configDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ConfigurarTiposEvaluacion_CursoNotOwnedByDocente_ReturnsForbid()
    {
        // Arrange
        SetupDocenteAuthentication(docenteId: 999);
        
        var configDto = new ConfigurarTiposEvaluacionDto
        {
            TiposEvaluacion = new List<ActualizarTipoEvaluacionDto>
            {
                new() { Id = 0, Nombre = "Test", Peso = 100, Orden = 1, Activo = true }
            }
        };

        // Act
        var result = await _controller.ConfigurarTiposEvaluacion(TestCursoId, configDto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
