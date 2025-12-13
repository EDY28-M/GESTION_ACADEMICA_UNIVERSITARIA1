using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Services;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Admin
{
    public class AdminControllerDocentesTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly GestionAcademicaContext _context;
        private readonly AdminController _controller;

        public AdminControllerDocentesTests()
        {
            _mockEstudianteService = new Mock<IEstudianteService>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            var adminService = new AdminService(_context, _mockEstudianteService.Object);
            _controller = new AdminController(adminService);
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Administrador")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupNonAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private async Task SeedDocentes()
        {
            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "P�rez",
                Profesion = "Ingeniero",
                Correo = "juan@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FechaCreacion = DateTime.Now
            };
            await _context.Docentes.AddAsync(docente);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetTodosDocentes_WithAdminUser_ReturnsOkWithDocentes()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.GetTodosDocentes();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetTodosDocentes_WithNonAdminUser_ReturnsForbid()
        {
            // Arrange
            SetupNonAdminUser();

            // Act
            var result = await _controller.GetTodosDocentes();

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CrearDocente_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();

            var dto = new CrearDocenteConPasswordDto
            {
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                Profesion = "Licenciada",
                Correo = "maria@test.com",
                Password = "password123"
            };

            // Act
            var result = await _controller.CrearDocente(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var docenteCreado = await _context.Docentes.FirstOrDefaultAsync(d => d.Correo == "maria@test.com");
            docenteCreado.Should().NotBeNull();
        }

        [Fact]
        public async Task CrearDocente_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var dto = new CrearDocenteConPasswordDto
            {
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                Profesion = "Licenciada",
                Correo = "juan@test.com", // Ya existe
                Password = "password123"
            };

            // Act
            var result = await _controller.CrearDocente(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearDocente_WithShortPassword_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var dto = new CrearDocenteConPasswordDto
            {
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                Profesion = "Licenciada",
                Correo = "maria@test.com",
                Password = "12345" // Menos de 6 caracteres
            };

            // Act
            var result = await _controller.CrearDocente(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ActualizarDocente_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var dto = new ActualizarDocenteDto
            {
                Nombres = "Juan Carlos",
                Apellidos = "P�rez L�pez",
                Profesion = "Doctor",
                Correo = "juancarlos@test.com"
            };

            // Act
            var result = await _controller.ActualizarDocente(1, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var docenteActualizado = await _context.Docentes.FindAsync(1);
            docenteActualizado!.Nombres.Should().Be("Juan Carlos");
        }

        [Fact]
        public async Task ActualizarDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var dto = new ActualizarDocenteDto
            {
                Nombres = "Test",
                Apellidos = "Test",
                Profesion = "Test"
            };

            // Act
            var result = await _controller.ActualizarDocente(999, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AsignarPasswordDocente_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var dto = new AsignarPasswordDto
            {
                Password = "newpassword123"
            };

            // Act
            var result = await _controller.AsignarPasswordDocente(1, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AsignarPasswordDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var dto = new AsignarPasswordDto
            {
                Password = "newpassword123"
            };

            // Act
            var result = await _controller.AsignarPasswordDocente(999, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AsignarPasswordDocente_WithShortPassword_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var dto = new AsignarPasswordDto
            {
                Password = "12345" // Menos de 6 caracteres
            };

            // Act
            var result = await _controller.AsignarPasswordDocente(1, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EliminarDocente_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            // Act
            var result = await _controller.EliminarDocente(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var docenteEliminado = await _context.Docentes.FindAsync(1);
            docenteEliminado.Should().BeNull();
        }

        [Fact]
        public async Task EliminarDocente_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.EliminarDocente(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task EliminarDocente_WithCursosAsignados_DesasignaCursos()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocentes();

            var curso = new Curso
            {
                Id = 1,
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 1 // Asignado al docente
            };
            await _context.Cursos.AddAsync(curso);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.EliminarDocente(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var cursoActualizado = await _context.Cursos.FindAsync(1);
            cursoActualizado!.IdDocente.Should().BeNull();
        }
    }
}
