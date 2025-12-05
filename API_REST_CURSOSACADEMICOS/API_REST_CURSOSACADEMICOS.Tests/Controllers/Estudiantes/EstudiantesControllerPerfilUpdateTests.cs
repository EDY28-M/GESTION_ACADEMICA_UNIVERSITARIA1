using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using System.Security.Claims;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Estudiantes;

/// <summary>
/// Tests para actualización de perfil y cambio de contraseña en EstudiantesController.
/// Valida seguridad y actualización de datos personales.
/// </summary>
public class EstudiantesControllerPerfilUpdateTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly EstudiantesController _controller;
    private readonly Mock<IEstudianteService> _estudianteServiceMock;

    private const int TestUsuarioId = 1;
    private const int TestEstudianteId = 1;
    private const string TestPassword = "password123";
    private const string TestPasswordHash = "$2a$11$K9s3FvMJk4qOT1LNvLYU8.K0.R0dWMG0wWvNmgZHCJZ9w8NvLxQgy"; // BCrypt hash de "password123"

    public EstudiantesControllerPerfilUpdateTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_PerfilUpdate_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _estudianteServiceMock = new Mock<IEstudianteService>();
        _controller = new EstudiantesController(_estudianteServiceMock.Object, _context);

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
        // Usar BCrypt para generar hash válido
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword);
        
        var usuario = new Usuario
        {
            Id = TestUsuarioId,
            Email = "estudiante@test.com",
            PasswordHash = passwordHash,
            Rol = "Estudiante"
        };

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
            Estado = "Activo",
            Telefono = "999888777",
            Direccion = "Av. Principal 123"
        };

        _context.Usuarios.Add(usuario);
        _context.Estudiantes.Add(estudiante);
        _context.SaveChanges();
    }

    #region Tests de CambiarContrasena

    [Fact]
    public async Task CambiarContrasena_WithValidData_ReturnsOk()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = TestPassword,
            ContrasenaNueva = "newPassword123"
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        
        // Verificar que la contraseña fue actualizada
        var usuario = await _context.Usuarios.FindAsync(TestUsuarioId);
        BCrypt.Net.BCrypt.Verify("newPassword123", usuario!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task CambiarContrasena_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = "wrongPassword",
            ContrasenaNueva = "newPassword123"
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarContrasena_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = TestPassword,
            ContrasenaNueva = "123" // Menos de 6 caracteres
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarContrasena_WithEmptyNewPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = TestPassword,
            ContrasenaNueva = ""
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarContrasena_WithUserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication(usuarioId: 999);
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = TestPassword,
            ContrasenaNueva = "newPassword123"
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CambiarContrasena_WithWhitespacePassword_ReturnsBadRequest()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var request = new CambiarContrasenaDto
        {
            ContrasenaActual = TestPassword,
            ContrasenaNueva = "      " // Solo espacios
        };

        // Act
        var result = await _controller.CambiarContrasena(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Tests de ActualizarPerfil

    [Fact]
    public async Task ActualizarPerfil_WithValidData_ReturnsOkWithUpdatedProfile()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        _estudianteServiceMock
            .Setup(s => s.GetByUsuarioIdAsync(TestUsuarioId))
            .ReturnsAsync(new EstudianteDto
            {
                Id = TestEstudianteId,
                Codigo = "2024001",
                Nombres = "María Actualizada",
                Apellidos = "García Modificada",
                Dni = "12345678"
            });
        
        var request = new ActualizarPerfilDto
        {
            Nombres = "María Actualizada",
            Apellidos = "García Modificada",
            Dni = "12345678",
            FechaNacimiento = new DateTime(2000, 5, 15),
            Correo = "nuevo@correo.com",
            Telefono = "111222333",
            Direccion = "Nueva Dirección 456"
        };

        // Act
        var result = await _controller.ActualizarPerfil(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        
        // Verificar que los datos fueron actualizados en BD
        var estudiante = await _context.Estudiantes.FindAsync(TestEstudianteId);
        estudiante!.Nombres.Should().Be("María Actualizada");
        estudiante.Apellidos.Should().Be("García Modificada");
        estudiante.Telefono.Should().Be("111222333");
        estudiante.Direccion.Should().Be("Nueva Dirección 456");
    }

    [Fact]
    public async Task ActualizarPerfil_WithStudentNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication(usuarioId: 999);
        
        var request = new ActualizarPerfilDto
        {
            Nombres = "Test",
            Apellidos = "Test",
            Dni = "00000000"
        };

        // Act
        var result = await _controller.ActualizarPerfil(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ActualizarPerfil_UpdatesOnlyProvidedFields()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var telefonoOriginal = "999888777";
        
        _estudianteServiceMock
            .Setup(s => s.GetByUsuarioIdAsync(TestUsuarioId))
            .ReturnsAsync(new EstudianteDto
            {
                Id = TestEstudianteId,
                Nombres = "María Solo Nombre",
                Apellidos = "García",
                Telefono = telefonoOriginal
            });
        
        var request = new ActualizarPerfilDto
        {
            Nombres = "María Solo Nombre",
            Apellidos = "García",
            Dni = "12345678",
            Telefono = "111222333", // Actualizar teléfono
            Direccion = "Av. Principal 123" // Mantener dirección
        };

        // Act
        var result = await _controller.ActualizarPerfil(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ActualizarPerfil_WithDateOfBirth_SavesCorrectly()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var fechaNacimiento = new DateTime(1998, 3, 25);
        
        _estudianteServiceMock
            .Setup(s => s.GetByUsuarioIdAsync(TestUsuarioId))
            .ReturnsAsync(new EstudianteDto { Id = TestEstudianteId });
        
        var request = new ActualizarPerfilDto
        {
            Nombres = "María",
            Apellidos = "García",
            Dni = "12345678",
            FechaNacimiento = fechaNacimiento
        };

        // Act
        var result = await _controller.ActualizarPerfil(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var estudiante = await _context.Estudiantes.FindAsync(TestEstudianteId);
        estudiante!.FechaNacimiento.Should().Be(fechaNacimiento);
    }

    #endregion

    #region Tests de GetRegistroNotas

    [Fact]
    public async Task GetRegistroNotas_WithValidStudent_ReturnsOk()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        _estudianteServiceMock
            .Setup(s => s.GetRegistroNotasAsync(TestEstudianteId))
            .ReturnsAsync(new RegistroNotasDto
            {
                Semestres = new List<SemestreRegistroDto>()
            });

        // Act
        var result = await _controller.GetRegistroNotas();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRegistroNotas_WithStudentNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication(usuarioId: 999);

        // Act
        var result = await _controller.GetRegistroNotas();

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Tests de GetRegistroNotasPorPeriodo

    [Fact]
    public async Task GetRegistroNotasPorPeriodo_WithValidData_ReturnsOk()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        var semestreRegistro = new SemestreRegistroDto
        {
            IdPeriodo = 1,
            Periodo = "2024-I",
            Totales = new TotalesSemestreDto 
            { 
                PromedioSemestral = 15.0m,
                TotalCreditos = 20
            },
            Cursos = new List<CursoRegistroDto>()
        };
        
        _estudianteServiceMock
            .Setup(s => s.GetRegistroNotasAsync(TestEstudianteId))
            .ReturnsAsync(new RegistroNotasDto
            {
                Semestres = new List<SemestreRegistroDto> { semestreRegistro }
            });

        // Act
        var result = await _controller.GetRegistroNotasPorPeriodo(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRegistroNotasPorPeriodo_WithInvalidPeriodo_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        _estudianteServiceMock
            .Setup(s => s.GetRegistroNotasAsync(TestEstudianteId))
            .ReturnsAsync(new RegistroNotasDto
            {
                Semestres = new List<SemestreRegistroDto>() // Sin semestres
            });

        // Act
        var result = await _controller.GetRegistroNotasPorPeriodo(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
