using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API_REST_CURSOSACADEMICOS.Tests.Services;

public class AsistenciaServiceTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly Mock<ILogger<AsistenciaService>> _loggerMock;
    private readonly AsistenciaService _service;

    public AsistenciaServiceTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"AsistenciaServiceDb_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _loggerMock = new Mock<ILogger<AsistenciaService>>();
        _service = new AsistenciaService(_context, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var usuario = new Usuario
        {
            Id = 1,
            Nombres = "Juan",
            Apellidos = "Perez",
            Email = "juan@test.com",
            Rol = "Estudiante",
            PasswordHash = "hash"
        };

        var estudiante = new Estudiante
        {
            Id = 1,
            IdUsuario = 1,
            Nombres = "Juan",
            Apellidos = "Perez",
            Codigo = "2024001",
            Correo = "juan@test.com"
        };

        var curso = new Curso
        {
            Id = 1,
            NombreCurso = "Matematica",
            Creditos = 4,
            Ciclo = 1
        };

        var matricula = new Matricula
        {
            Id = 1,
            IdEstudiante = 1,
            IdCurso = 1,
            Estado = "Matriculado",
            IdPeriodo = 1
        };
        
        var periodo = new Periodo
        {
            Id = 1,
            Nombre = "2024-I",
            Activo = true,
            FechaInicio = DateTime.Today.AddMonths(-1),
            FechaFin = DateTime.Today.AddMonths(5)
        };

        _context.Usuarios.Add(usuario);
        _context.Estudiantes.Add(estudiante);
        _context.Cursos.Add(curso);
        _context.Matriculas.Add(matricula);
        _context.Periodos.Add(periodo);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RegistrarAsistencia_ValidData_CreatesRecord()
    {
        // Arrange
        var dto = new RegistrarAsistenciaDto
        {
            IdEstudiante = 1,
            IdCurso = 1,
            Fecha = DateTime.Today,
            TipoClase = "Teoria",
            Presente = true,
            Observaciones = "Test"
        };

        // Act
        var result = await _service.RegistrarAsistenciaAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Presente.Should().BeTrue();
        
        var dbRecord = await _context.Asistencias.FindAsync(result.Id);
        dbRecord.Should().NotBeNull();
        dbRecord!.TipoClase.Should().Be("Teoria");
    }

    [Fact]
    public async Task RegistrarAsistencia_Duplicate_ThrowsException()
    {
        // Arrange
        var dto = new RegistrarAsistenciaDto
        {
            IdEstudiante = 1,
            IdCurso = 1,
            Fecha = DateTime.Today,
            TipoClase = "Teoria",
            Presente = true
        };

        await _service.RegistrarAsistenciaAsync(dto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RegistrarAsistenciaAsync(dto));
    }

    [Fact]
    public async Task RegistrarAsistenciasMasivas_MixedNewAndExisting_UpdatesAndCreates()
    {
        // Arrange
        // Crear una asistencia previa
        _context.Asistencias.Add(new Asistencia
        {
            IdEstudiante = 1,
            IdCurso = 1,
            Fecha = DateTime.Today,
            TipoClase = "Teoria",
            Presente = false // Estaba ausente
        });
        await _context.SaveChangesAsync();

        var dto = new RegistrarAsistenciasMasivasDto
        {
            IdCurso = 1,
            Fecha = DateTime.Today,
            TipoClase = "Teoria",
            Estudiantes = new List<AsistenciaEstudianteDto>
            {
                new() { IdEstudiante = 1, Presente = true } // Ahora presente (Update)
            }
        };

        // Act
        var result = await _service.RegistrarAsistenciasMasivasAsync(dto);

        // Assert
        result.Should().HaveCount(1);
        result.First().Presente.Should().BeTrue();
        
        var dbRecord = await _context.Asistencias.FirstOrDefaultAsync(a => a.IdEstudiante == 1);
        dbRecord!.Presente.Should().BeTrue();
    }

    [Fact]
    public async Task GetResumenAsistenciaCurso_CalculatesPercentagesCorrectly()
    {
        // Arrange
        // Agregar 2 clases: 1 presente, 1 falta -> 50% asistencia
        _context.Asistencias.AddRange(
            new Asistencia { IdEstudiante = 1, IdCurso = 1, Fecha = DateTime.Today.AddDays(-1), Presente = true, TipoClase = "Teoria" },
            new Asistencia { IdEstudiante = 1, IdCurso = 1, Fecha = DateTime.Today, Presente = false, TipoClase = "Teoria" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetResumenAsistenciaCursoAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.TotalClases.Should().Be(2);
        
        var estudianteResumen = result.Estudiantes.First(e => e.IdEstudiante == 1);
        estudianteResumen.TotalAsistencias.Should().Be(1);
        estudianteResumen.TotalFaltas.Should().Be(1);
        estudianteResumen.PorcentajeAsistencia.Should().Be(50.0m);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
