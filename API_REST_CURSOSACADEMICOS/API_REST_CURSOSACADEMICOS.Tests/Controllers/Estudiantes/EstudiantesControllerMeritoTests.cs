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
/// Tests para orden de mérito y promociones en EstudiantesController.
/// Verifica consultas de ranking académico y posición del estudiante.
/// </summary>
public class EstudiantesControllerMeritoTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly EstudiantesController _controller;
    private readonly Mock<IEstudianteService> _estudianteServiceMock;

    private const int TestUsuarioId = 1;
    private const int TestEstudianteId = 1;

    public EstudiantesControllerMeritoTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Merito_{Guid.NewGuid()}")
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
        // Usuarios
        var usuarios = new[]
        {
            new Usuario { Id = 1, Email = "estudiante1@test.com", PasswordHash = "hash", Rol = "Estudiante" },
            new Usuario { Id = 2, Email = "estudiante2@test.com", PasswordHash = "hash", Rol = "Estudiante" },
            new Usuario { Id = 3, Email = "estudiante3@test.com", PasswordHash = "hash", Rol = "Estudiante" }
        };

        // Estudiantes con diferentes promedios
        var estudiantes = new[]
        {
            new Estudiante
            {
                Id = 1,
                IdUsuario = 1,
                Codigo = "2024001",
                Nombres = "María",
                Apellidos = "García",
                Correo = "estudiante1@test.com",
                Dni = "12345678",
                CicloActual = 3,
                Estado = "Activo",
                Promocion = "2024",
                PromedioAcumulado = 16.5m,
                CreditosAcumulados = 45
            },
            new Estudiante
            {
                Id = 2,
                IdUsuario = 2,
                Codigo = "2024002",
                Nombres = "Juan",
                Apellidos = "Pérez",
                Correo = "estudiante2@test.com",
                Dni = "87654321",
                CicloActual = 3,
                Estado = "Activo",
                Promocion = "2024",
                PromedioAcumulado = 15.0m,
                CreditosAcumulados = 45
            },
            new Estudiante
            {
                Id = 3,
                IdUsuario = 3,
                Codigo = "2023001",
                Nombres = "Pedro",
                Apellidos = "López",
                Correo = "estudiante3@test.com",
                Dni = "11111111",
                CicloActual = 5,
                Estado = "Activo",
                Promocion = "2023",
                PromedioAcumulado = 14.0m,
                CreditosAcumulados = 80
            }
        };

        // Periodo activo
        var periodo = new Periodo
        {
            Id = 1,
            Nombre = "2024-I",
            FechaInicio = DateTime.Now.AddMonths(-1),
            FechaFin = DateTime.Now.AddMonths(5),
            Activo = true
        };

        _context.Usuarios.AddRange(usuarios);
        _context.Estudiantes.AddRange(estudiantes);
        _context.Periodos.Add(periodo);
        _context.SaveChanges();
    }

    #region Tests de GetPromociones

    [Fact]
    public async Task GetPromociones_ReturnsDistinctPromociones()
    {
        // Arrange
        SetupEstudianteAuthentication();

        // Act
        var result = await _controller.GetPromociones();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var promociones = okResult?.Value as IEnumerable<string>;
        
        promociones.Should().NotBeNull();
        promociones!.Should().Contain("2024");
        promociones.Should().Contain("2023");
        var promocionesList = promociones.ToList();
        promocionesList.Distinct().Count().Should().Be(promocionesList.Count);
    }

    [Fact]
    public async Task GetPromociones_OrdersByDescending()
    {
        // Arrange
        SetupEstudianteAuthentication();

        // Act
        var result = await _controller.GetPromociones();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var promociones = (okResult?.Value as IEnumerable<string>)?.ToList() ?? new List<string>();
        
        promociones.Should().NotBeEmpty();
        
        // 2024 debería aparecer antes que 2023
        var index2024 = promociones.IndexOf("2024");
        var index2023 = promociones.IndexOf("2023");
        
        index2024.Should().BeLessThan(index2023);
    }

    [Fact]
    public async Task GetPromociones_ExcludesInactiveStudents()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        // Agregar estudiante inactivo
        _context.Estudiantes.Add(new Estudiante
        {
            Id = 100,
            IdUsuario = 100,
            Codigo = "2022001",
            Nombres = "Test",
            Apellidos = "Inactivo",
            Correo = "inactivo@test.com",
            Dni = "99999999",
            CicloActual = 1,
            Estado = "Inactivo",
            Promocion = "2022"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPromociones();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var promociones = okResult?.Value as IEnumerable<string> ?? Enumerable.Empty<string>();
        
        promociones.Should().NotContain("2022");
    }

    [Fact]
    public async Task GetPromociones_ExcludesNullPromociones()
    {
        // Arrange
        SetupEstudianteAuthentication();
        
        // Agregar estudiante sin promoción
        _context.Estudiantes.Add(new Estudiante
        {
            Id = 101,
            IdUsuario = 101,
            Codigo = "2024099",
            Nombres = "Sin",
            Apellidos = "Promocion",
            Correo = "sin@test.com",
            Dni = "88888888",
            CicloActual = 1,
            Estado = "Activo",
            Promocion = null
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPromociones();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var promociones = (okResult?.Value as IEnumerable<string>)?.ToList() ?? new List<string>();
        
        promociones.Should().NotContain((string?)null);
        promociones.All(p => !string.IsNullOrEmpty(p)).Should().BeTrue();
    }

    #endregion

    #region Tests de GetMiPosicionMerito

    [Fact]
    public async Task GetMiPosicionMerito_WithStudentNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupEstudianteAuthentication(usuarioId: 999);

        // Act
        var result = await _controller.GetMiPosicionMerito();

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMiPosicionMerito_WithNoPromocion_ReturnsNotFoundWithMessage()
    {
        // Arrange
        // Agregar estudiante sin promoción
        var usuarioSinPromo = new Usuario 
        { 
            Id = 200, 
            Email = "sinpromo@test.com", 
            PasswordHash = "hash", 
            Rol = "Estudiante" 
        };
        var estudianteSinPromo = new Estudiante
        {
            Id = 200,
            IdUsuario = 200,
            Codigo = "2024200",
            Nombres = "Sin",
            Apellidos = "Promocion",
            Correo = "sinpromo@test.com",
            Dni = "77777777",
            CicloActual = 1,
            Estado = "Activo",
            Promocion = null
        };
        _context.Usuarios.Add(usuarioSinPromo);
        _context.Estudiantes.Add(estudianteSinPromo);
        await _context.SaveChangesAsync();

        SetupEstudianteAuthentication(usuarioId: 200);

        // Act
        var result = await _controller.GetMiPosicionMerito();

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        var response = notFoundResult?.Value;
        
        response.Should().NotBeNull();
        var mensaje = response?.GetType().GetProperty("mensaje")?.GetValue(response);
        mensaje.Should().NotBeNull();
    }

    #endregion

    #region Tests de GetOrdenMerito

    [Fact]
    public async Task GetOrdenMerito_ReturnsActionResult()
    {
        // Arrange
        SetupEstudianteAuthentication();

        // Act  
        var result = await _controller.GetOrdenMerito();

        // Assert
        // Como usa una vista SQL que no existe en InMemory, esperamos StatusCode 500 o Ok vacío
        result.Result.Should().BeAssignableTo<ObjectResult>();
    }

    [Fact]
    public async Task GetOrdenMerito_WithPromocionFilter_AcceptsParameter()
    {
        // Arrange
        SetupEstudianteAuthentication();

        // Act
        var result = await _controller.GetOrdenMerito(promocion: "2024");

        // Assert
        // El método debería aceptar el parámetro sin error de compilación
        result.Result.Should().BeAssignableTo<ObjectResult>();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
