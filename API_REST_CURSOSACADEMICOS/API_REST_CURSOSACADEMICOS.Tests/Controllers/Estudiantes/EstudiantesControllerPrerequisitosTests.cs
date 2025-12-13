using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using System.Security.Claims;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Services;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Estudiantes;

/// <summary>
/// Tests para verificación de prerequisitos en EstudiantesController.
/// Valida la lógica de verificación de cursos prerequisito para matrícula.
/// </summary>
public class EstudiantesControllerPrerequisitosTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly EstudiantesController _controller;
    private readonly Mock<IEstudianteService> _estudianteServiceMock;

    private const int TestUsuarioId = 1;
    private const int TestEstudianteId = 1;
    private const int TestCursoId = 1;
    private const int TestCursoPrerequisito1Id = 2;
    private const int TestCursoPrerequisito2Id = 3;
    private const int TestPeriodoId = 1;

    public EstudiantesControllerPrerequisitosTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Prerequisitos_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _estudianteServiceMock = new Mock<IEstudianteService>();
        var controllerService = new EstudiantesControllerService(_context, _estudianteServiceMock.Object);
        _controller = new EstudiantesController(_estudianteServiceMock.Object, controllerService);

        SeedTestData();
    }

    private void SetupEstudianteAuthentication(int usuarioId = TestUsuarioId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Role, "Estudiante"),
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
        // Usuario
        var usuario = new Usuario
        {
            Id = TestUsuarioId,
            Email = "estudiante@test.com",
            PasswordHash = "hashedPassword",
            Rol = "Estudiante"
        };

        // Periodo activo
        var periodo = new Periodo
        {
            Id = TestPeriodoId,
            Nombre = "2024-I",
            FechaInicio = DateTime.Now.AddMonths(-1),
            FechaFin = DateTime.Now.AddMonths(5),
            Activo = true
        };

        // Estudiante
        var estudiante = new Estudiante
        {
            Id = TestEstudianteId,
            IdUsuario = TestUsuarioId,
            Codigo = "2024001",
            Nombres = "María",
            Apellidos = "García",
            Correo = "estudiante@test.com",
            Dni = "12345678",
            CicloActual = 3,
            Estado = "Activo"
        };

        // Cursos
        var cursoConPrerequisitos = new Curso
        {
            Id = TestCursoId,
            Codigo = "CS301",
            NombreCurso = "Estructuras de Datos",
            Creditos = 4,
            HorasSemanal = 6,
            Ciclo = 3
        };

        var cursoPrerequisito1 = new Curso
        {
            Id = TestCursoPrerequisito1Id,
            Codigo = "CS101",
            NombreCurso = "Programación I",
            Creditos = 4,
            HorasSemanal = 6,
            Ciclo = 1
        };

        var cursoPrerequisito2 = new Curso
        {
            Id = TestCursoPrerequisito2Id,
            Codigo = "CS102",
            NombreCurso = "Programación II",
            Creditos = 4,
            HorasSemanal = 6,
            Ciclo = 2
        };

        // Prerequisitos
        var prerequisitos = new[]
        {
            new CursoPrerequisito { IdCurso = TestCursoId, IdCursoPrerequisito = TestCursoPrerequisito1Id },
            new CursoPrerequisito { IdCurso = TestCursoId, IdCursoPrerequisito = TestCursoPrerequisito2Id }
        };

        _context.Usuarios.Add(usuario);
        _context.Periodos.Add(periodo);
        _context.Estudiantes.Add(estudiante);
        _context.Cursos.AddRange(cursoConPrerequisitos, cursoPrerequisito1, cursoPrerequisito2);
        _context.CursoPrerequisitos.AddRange(prerequisitos);
        _context.SaveChanges();
    }

    private void SetupEstudianteServiceMock()
    {
        _estudianteServiceMock
            .Setup(s => s.GetByUsuarioIdAsync(TestUsuarioId))
            .ReturnsAsync(new DTOs.EstudianteDto
            {
                Id = TestEstudianteId,
                Codigo = "2024001",
                Nombres = "María",
                Apellidos = "García",
                CicloActual = 3
            });
    }

    #region Tests de VerificarPrerequisitos

    [Fact]
    public async Task VerificarPrerequisitos_WithNoPrerequisites_ReturnsCumpleTodos()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // Crear curso sin prerequisitos
        var cursoSinPrerequisitos = new Curso
        {
            Id = 100,
            Codigo = "GEN001",
            NombreCurso = "Curso General",
            Creditos = 2,
            HorasSemanal = 3,
            Ciclo = 1
        };
        _context.Cursos.Add(cursoSinPrerequisitos);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.VerificarPrerequisitos(100);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        // Verificar que cumple prerequisitos usando reflexión para objetos anónimos
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(true);
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithAllPrerequisitesApproved_ReturnsCumpleTodos()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // Agregar matrículas aprobadas para los prerequisitos
        _context.Matriculas.AddRange(new[]
        {
            new Matricula
            {
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoPrerequisito1Id,
                IdPeriodo = TestPeriodoId,
                Estado = "Aprobado",
                PromedioFinal = 15
            },
            new Matricula
            {
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoPrerequisito2Id,
                IdPeriodo = TestPeriodoId,
                Estado = "Aprobado",
                PromedioFinal = 14
            }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(true);
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithMissingPrerequisites_ReturnsNoConCumple()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // No agregar matrículas - el estudiante no ha cursado los prerequisitos

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(false);
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithFailedPrerequisite_ReturnsNoCumple()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // Agregar una matrícula aprobada y una reprobada
        _context.Matriculas.AddRange(new[]
        {
            new Matricula
            {
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoPrerequisito1Id,
                IdPeriodo = TestPeriodoId,
                Estado = "Aprobado",
                PromedioFinal = 15
            },
            new Matricula
            {
                IdEstudiante = TestEstudianteId,
                IdCurso = TestCursoPrerequisito2Id,
                IdPeriodo = TestPeriodoId,
                Estado = "Desaprobado",
                PromedioFinal = 8 // Nota menor a 11
            }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(false);
        
        // Verificar que devuelve la lista de prerequisitos faltantes
        var faltantes = response!.GetType().GetProperty("prerequisitosFaltantes")?.GetValue(response);
        faltantes.Should().NotBeNull();
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithStudentNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication(usuarioId: 999);
        
        _estudianteServiceMock
            .Setup(s => s.GetByUsuarioIdAsync(999))
            .ReturnsAsync((DTOs.EstudianteDto?)null);

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithPrerequisiteInProgress_ReturnsNoCumpleWithStatus()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // Agregar matrícula sin promedio final (en curso)
        _context.Matriculas.Add(new Matricula
        {
            IdEstudiante = TestEstudianteId,
            IdCurso = TestCursoPrerequisito1Id,
            IdPeriodo = TestPeriodoId,
            Estado = "Matriculado",
            PromedioFinal = null // Aún cursando
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(false);
    }

    [Fact]
    public async Task VerificarPrerequisitos_WithPartialPrerequisitesApproved_ReturnsNoCumple()
    {
        // Arrange
        SetupEstudianteAuthentication();
        SetupEstudianteServiceMock();

        // Solo aprobar uno de los dos prerequisitos
        _context.Matriculas.Add(new Matricula
        {
            IdEstudiante = TestEstudianteId,
            IdCurso = TestCursoPrerequisito1Id,
            IdPeriodo = TestPeriodoId,
            Estado = "Aprobado",
            PromedioFinal = 15
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.VerificarPrerequisitos(TestCursoId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value;
        
        var cumple = response!.GetType().GetProperty("cumplePrerequisitos")?.GetValue(response);
        cumple.Should().Be(false);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
