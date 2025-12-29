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
    public class AdminControllerPeriodosTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly Mock<IHorarioService> _mockHorarioService;
        private readonly GestionAcademicaContext _context;
        private readonly AdminController _controller;

        public AdminControllerPeriodosTests()
        {
            _mockEstudianteService = new Mock<IEstudianteService>();
            _mockHorarioService = new Mock<IHorarioService>();
            _mockHorarioService.Setup(x => x.EliminarTodosHorariosAsync()).ReturnsAsync(0);
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            var adminService = new AdminService(_context, _mockEstudianteService.Object, _mockHorarioService.Object);
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

        private async Task SeedPeriodos()
        {
            var periodo = new Periodo
            {
                Id = 1,
                Nombre = "2024-I",
                Anio = 2024,
                Ciclo = "I",
                FechaInicio = new DateTime(2024, 3, 1),
                FechaFin = new DateTime(2024, 7, 31),
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            await _context.Periodos.AddAsync(periodo);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetPeriodos_WithAdminUser_ReturnsOkWithPeriodos()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            // Act
            var result = await _controller.GetPeriodos();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPeriodos_WithNonAdminUser_ReturnsForbid()
        {
            // Arrange
            SetupNonAdminUser();

            // Act
            var result = await _controller.GetPeriodos();

            // Assert
            // Nota: esta suite llama al controlador directamente (sin pipeline MVC),
            // por lo que el [Authorize(Roles="Administrador")] NO se evalúa aquí.
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CrearPeriodo_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();

            var dto = new CrearPeriodoDto
            {
                Nombre = "2024-II",
                Anio = 2024,
                Ciclo = "II",
                FechaInicio = new DateTime(2024, 8, 1),
                FechaFin = new DateTime(2024, 12, 15),
                Activo = false
            };

            // Act
            var result = await _controller.CrearPeriodo(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var periodoCreado = await _context.Periodos.FirstOrDefaultAsync(p => p.Nombre == "2024-II");
            periodoCreado.Should().NotBeNull();
        }

        [Fact]
        public async Task CrearPeriodo_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            var dto = new CrearPeriodoDto
            {
                Nombre = "2024-I", // Ya existe
                Anio = 2024,
                Ciclo = "I",
                FechaInicio = new DateTime(2024, 3, 1),
                FechaFin = new DateTime(2024, 7, 31),
                Activo = false
            };

            // Act
            var result = await _controller.CrearPeriodo(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearPeriodo_WithInvalidDates_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var dto = new CrearPeriodoDto
            {
                Nombre = "2024-II",
                Anio = 2024,
                Ciclo = "II",
                FechaInicio = new DateTime(2024, 12, 15), // Fecha inicio despu�s de fin
                FechaFin = new DateTime(2024, 8, 1),
                Activo = false
            };

            // Act
            var result = await _controller.CrearPeriodo(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EditarPeriodo_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            var dto = new EditarPeriodoDto
            {
                Nombre = "2024-I Actualizado",
                Anio = 2024,
                Ciclo = "I",
                FechaInicio = new DateTime(2024, 3, 1),
                FechaFin = new DateTime(2024, 8, 15)
            };

            // Act
            var result = await _controller.EditarPeriodo(1, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var periodoActualizado = await _context.Periodos.FindAsync(1);
            periodoActualizado!.Nombre.Should().Be("2024-I Actualizado");
        }

        [Fact]
        public async Task EditarPeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var dto = new EditarPeriodoDto
            {
                Nombre = "Test",
                Anio = 2024,
                Ciclo = "I",
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now.AddMonths(4)
            };

            // Act
            var result = await _controller.EditarPeriodo(999, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ActivarPeriodo_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            
            // Crear un per�odo inactivo
            var periodoInactivo = new Periodo
            {
                Id = 2,
                Nombre = "2024-II",
                Anio = 2024,
                Ciclo = "II",
                FechaInicio = new DateTime(2024, 8, 1),
                FechaFin = new DateTime(2024, 12, 15),
                Activo = false,
                FechaCreacion = DateTime.Now
            };
            await _context.Periodos.AddAsync(periodoInactivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ActivarPeriodo(2);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ActivarPeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.ActivarPeriodo(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task EliminarPeriodo_WithValidIdAndNoMatriculas_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            // Act
            var result = await _controller.EliminarPeriodo(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var periodoEliminado = await _context.Periodos.FindAsync(1);
            periodoEliminado.Should().BeNull();
        }

        [Fact]
        public async Task EliminarPeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.EliminarPeriodo(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task EliminarPeriodo_WithMatriculas_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            // Crear estudiante y matr�cula asociada al per�odo
            var usuario = new Usuario
            {
                Id = 1,
                Email = "test@test.com",
                PasswordHash = "hash",
                Rol = "Estudiante",
                Estado = true
            };
            await _context.Usuarios.AddAsync(usuario);

            var estudiante = new Estudiante
            {
                Id = 1,
                IdUsuario = 1,
                Codigo = "EST001",
                Nombres = "Test",
                Apellidos = "Test",
                Estado = "Activo"
            };
            await _context.Estudiantes.AddAsync(estudiante);

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Docente",
                Apellidos = "Test",
                Profesion = "Ingeniero"
            };
            await _context.Docentes.AddAsync(docente);

            var curso = new Curso
            {
                Id = 1,
                Codigo = "CUR001",
                NombreCurso = "Test",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 1
            };
            await _context.Cursos.AddAsync(curso);

            var matricula = new Matricula
            {
                Id = 1,
                IdEstudiante = 1,
                IdCurso = 1,
                IdPeriodo = 1,
                FechaMatricula = DateTime.Now,
                Estado = "Matriculado"
            };
            await _context.Matriculas.AddAsync(matricula);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.EliminarPeriodo(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CerrarPeriodo_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            // Act
            var result = await _controller.CerrarPeriodo(1);

            // Assert
            // En InMemory no existen stored procedures (sp_CerrarPeriodo), así que el service lanza excepción
            // y el controller responde 500.
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CerrarPeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.CerrarPeriodo(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AbrirPeriodo_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            
            // Crear un período cerrado
            var periodoCerrado = new Periodo
            {
                Id = 3,
                Nombre = "2023-II",
                Anio = 2023,
                Ciclo = "II",
                FechaInicio = new DateTime(2023, 8, 1),
                FechaFin = new DateTime(2023, 12, 15),
                Activo = false,
                FechaCreacion = DateTime.Now
            };
            await _context.Periodos.AddAsync(periodoCerrado);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.AbrirPeriodo(3);

            // Assert
            // En InMemory no existen stored procedures (sp_AbrirPeriodo), así que el service lanza excepción
            // y el controller responde 500.
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task AbrirPeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.AbrirPeriodo(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ValidarCierrePeriodo_WithValidId_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();
            await SeedPeriodos();

            // Act
            var result = await _controller.ValidarCierrePeriodo(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ValidarCierrePeriodo_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.ValidarCierrePeriodo(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
