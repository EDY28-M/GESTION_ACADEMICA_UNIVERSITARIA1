using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Tests;

/// <summary>
/// Pruebas unitarias para HealthController
/// </summary>
public class HealthControllerTests
{
    private readonly GestionAcademicaContext _context;
    private readonly Mock<ILogger<HealthController>> _loggerMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        // Configurar InMemory Database
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new GestionAcademicaContext(options);
        _loggerMock = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_context, _loggerMock.Object);
    }

    [Fact]
    public void Get_ReturnsOkResult_WithHealthStatus()
    {
        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void Live_ReturnsOkResult_WithAliveStatus()
    {
        // Act
        var result = _controller.Live();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetDetailed_ReturnsResult_WithDetailedHealth()
    {
        // Act
        var result = await _controller.GetDetailed();

        // Assert
        Assert.NotNull(result);
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task Ready_ReturnsOkResult_WhenDatabaseIsAccessible()
    {
        // Act
        var result = await _controller.Ready();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}

/// <summary>
/// Pruebas unitarias para CursosController
/// </summary>
public class CursosControllerTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly CursosController _controller;

    public CursosControllerTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new GestionAcademicaContext(options);
        _controller = new CursosController(_context);

        // Seed data
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var docente = new Docente
        {
            Id = 1,
            Nombres = "Juan",
            Apellidos = "Pérez",
            Profesion = "Ingeniero",
            Correo = "juan.perez@test.com"
        };
        _context.Docentes.Add(docente);

        var cursos = new List<Curso>
        {
            new Curso { Id = 1, Codigo = "MAT101", NombreCurso = "Matemáticas I", Creditos = 4, HorasSemanal = 5, Ciclo = 1, IdDocente = 1 },
            new Curso { Id = 2, Codigo = "MAT102", NombreCurso = "Matemáticas II", Creditos = 4, HorasSemanal = 5, Ciclo = 2, IdDocente = 1 },
            new Curso { Id = 3, Codigo = "FIS101", NombreCurso = "Física I", Creditos = 3, HorasSemanal = 4, Ciclo = 1, IdDocente = 1 }
        };
        _context.Cursos.AddRange(cursos);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetCursos_ReturnsOkResult_WithListOfCursos()
    {
        // Act
        var result = await _controller.GetCursos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var cursos = Assert.IsAssignableFrom<IEnumerable<CursoDto>>(okResult.Value);
        Assert.Equal(3, cursos.Count());
    }

    [Fact]
    public async Task GetCurso_WithValidId_ReturnsOkResult()
    {
        // Act
        var result = await _controller.GetCurso(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var curso = Assert.IsType<CursoDto>(okResult.Value);
        Assert.Equal("Matemáticas I", curso.NombreCurso);
    }

    [Fact]
    public async Task GetCurso_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetCurso(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCursosPorCiclo_ReturnsCorrectCursos()
    {
        // Act
        var result = await _controller.GetCursosPorCiclo(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var cursos = Assert.IsAssignableFrom<IEnumerable<CursoDto>>(okResult.Value);
        Assert.Equal(2, cursos.Count()); // MAT101 y FIS101 son ciclo 1
    }

    [Fact]
    public async Task GetCursosPorDocente_WithValidId_ReturnsCursos()
    {
        // Act
        var result = await _controller.GetCursosPorDocente(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var cursos = Assert.IsAssignableFrom<IEnumerable<CursoDto>>(okResult.Value);
        Assert.Equal(3, cursos.Count());
    }

    [Fact]
    public async Task GetCursosPorDocente_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetCursosPorDocente(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PostCurso_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var cursoDto = new CursoCreateDto
        {
            Codigo = "QUI101",
            NombreCurso = "Química I",
            Creditos = 3,
            HorasSemanal = 4,
            Ciclo = 1,
            IdDocente = 1,
            PrerequisitosIds = new List<int>()
        };

        // Act
        var result = await _controller.PostCurso(cursoDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var curso = Assert.IsType<CursoDto>(createdResult.Value);
        Assert.Equal("Química I", curso.NombreCurso);
    }

    [Fact]
    public async Task PostCurso_WithInvalidDocente_ReturnsBadRequest()
    {
        // Arrange
        var cursoDto = new CursoCreateDto
        {
            Codigo = "QUI101",
            NombreCurso = "Química I",
            Creditos = 3,
            HorasSemanal = 4,
            Ciclo = 1,
            IdDocente = 999, // Docente no existe
            PrerequisitosIds = new List<int>()
        };

        // Act
        var result = await _controller.PostCurso(cursoDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PutCurso_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var cursoDto = new CursoUpdateDto
        {
            Codigo = "MAT101",
            NombreCurso = "Matemáticas I - Actualizado",
            Creditos = 5,
            HorasSemanal = 6,
            Ciclo = 1,
            IdDocente = 1,
            PrerequisitosIds = new List<int>()
        };

        // Act
        var result = await _controller.PutCurso(1, cursoDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verificar actualización
        var cursoActualizado = await _context.Cursos.FindAsync(1);
        Assert.Equal("Matemáticas I - Actualizado", cursoActualizado!.NombreCurso);
        Assert.Equal(5, cursoActualizado.Creditos);
    }

    [Fact]
    public async Task PutCurso_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cursoDto = new CursoUpdateDto
        {
            Codigo = "MAT101",
            NombreCurso = "Matemáticas I",
            Creditos = 4,
            HorasSemanal = 5,
            Ciclo = 1,
            PrerequisitosIds = new List<int>()
        };

        // Act
        var result = await _controller.PutCurso(999, cursoDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCurso_WithValidId_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteCurso(1);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verificar eliminación
        var cursoEliminado = await _context.Cursos.FindAsync(1);
        Assert.Null(cursoEliminado);
    }

    [Fact]
    public async Task DeleteCurso_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteCurso(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

/// <summary>
/// Pruebas de integración básicas
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void CursoModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var curso = new Curso();

        // Assert
        Assert.Equal(0, curso.Id);
        Assert.Equal(string.Empty, curso.NombreCurso);
        Assert.NotNull(curso.PrerequisitosRequeridos);
        Assert.Empty(curso.PrerequisitosRequeridos);
    }

    [Fact]
    public void CursoDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new CursoDto
        {
            Id = 1,
            Codigo = "TEST101",
            NombreCurso = "Test Course",
            Creditos = 3,
            HorasSemanal = 4,
            Ciclo = 1
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("TEST101", dto.Codigo);
        Assert.Equal("Test Course", dto.NombreCurso);
    }

    [Fact]
    public void CursoCreateDto_Validation_RequiredFields()
    {
        // Arrange
        var dto = new CursoCreateDto
        {
            NombreCurso = "Test",
            Creditos = 3,
            HorasSemanal = 4,
            Ciclo = 1
        };

        // Assert - Should have valid values
        Assert.Equal("Test", dto.NombreCurso);
        Assert.Equal(3, dto.Creditos);
        Assert.NotNull(dto.PrerequisitosIds);
    }

    [Fact]
    public void DocenteModel_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var docente = new Docente
        {
            Id = 1,
            Nombres = "Juan",
            Apellidos = "Pérez",
            Profesion = "Ingeniero",
            Correo = "juan@test.com"
        };

        // Assert
        Assert.Equal(1, docente.Id);
        Assert.Equal("Juan", docente.Nombres);
        Assert.Equal("Pérez", docente.Apellidos);
    }
}

/// <summary>
/// Pruebas para validación de DTOs
/// </summary>
public class DtoValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CursoCreateDto_InvalidCreditos_ShouldFail(int creditos)
    {
        // Arrange
        var dto = new CursoCreateDto
        {
            NombreCurso = "Test",
            Creditos = creditos,
            HorasSemanal = 4,
            Ciclo = 1
        };

        // Act - Validate using DataAnnotations
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CursoCreateDto_ValidCreditos_ShouldPass(int creditos)
    {
        // Arrange
        var dto = new CursoCreateDto
        {
            NombreCurso = "Test",
            Creditos = creditos,
            HorasSemanal = 4,
            Ciclo = 1
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void CursoCreateDto_InvalidCiclo_ShouldFail(int ciclo)
    {
        // Arrange
        var dto = new CursoCreateDto
        {
            NombreCurso = "Test",
            Creditos = 3,
            HorasSemanal = 4,
            Ciclo = ciclo
        };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
    }
}
