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
    public class AdminControllerCursosDirigidosTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly GestionAcademicaContext _context;
        private readonly AdminController _controller;

        public AdminControllerCursosDirigidosTests()
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

        private async Task SeedDataForCursosDirigidos()
        {
            // Periodo
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

            // Docente
            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "P�rez",
                Profesion = "Ingeniero"
            };
            await _context.Docentes.AddAsync(docente);

            // Curso
            var curso = new Curso
            {
                Id = 1,
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 3,
                IdDocente = 1
            };
            await _context.Cursos.AddAsync(curso);

            // Usuario y Estudiante
            var usuario = new Usuario
            {
                Id = 1,
                Email = "estudiante@test.com",
                PasswordHash = "hash",
                Rol = "Estudiante",
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                Estado = true
            };
            await _context.Usuarios.AddAsync(usuario);

            var estudiante = new Estudiante
            {
                Id = 1,
                IdUsuario = 1,
                Codigo = "EST001",
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                CicloActual = 1, // Ciclo diferente al curso
                Estado = "Activo"
            };
            await _context.Estudiantes.AddAsync(estudiante);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task CrearCursosDirigidos_WithNonAdminUser_ReturnsForbid()
        {
            // Arrange
            SetupNonAdminUser();

            var dto = new MatriculaDirigidaDto
            {
                IdCurso = 1,
                IdPeriodo = 1,
                IdsEstudiantes = new List<int> { 1 }
            };

            // Act
            var result = await _controller.CrearCursosDirigidos(dto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CrearCursosDirigidos_WithInvalidPeriodo_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDataForCursosDirigidos();

            var dto = new MatriculaDirigidaDto
            {
                IdCurso = 1,
                IdPeriodo = 999, // Per�odo no existe
                IdsEstudiantes = new List<int> { 1 }
            };

            // Act
            var result = await _controller.CrearCursosDirigidos(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearCursosDirigidos_WithInvalidCurso_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDataForCursosDirigidos();

            var dto = new MatriculaDirigidaDto
            {
                IdCurso = 999, // Curso no existe
                IdPeriodo = 1,
                IdsEstudiantes = new List<int> { 1 }
            };

            // Act
            var result = await _controller.CrearCursosDirigidos(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearCursosDirigidos_WithValidData_ReturnsOkWithResults()
        {
            // Arrange
            SetupAdminUser();
            await SeedDataForCursosDirigidos();

            var matriculaDto = new MatriculaDto
            {
                Id = 1,
                IdEstudiante = 1,
                IdCurso = 1,
                IdPeriodo = 1,
                Estado = "Matriculado"
            };

            _mockEstudianteService
                .Setup(s => s.MatricularAsync(1, It.IsAny<MatricularDto>(), true))
                .ReturnsAsync(matriculaDto);

            var dto = new MatriculaDirigidaDto
            {
                IdCurso = 1,
                IdPeriodo = 1,
                IdsEstudiantes = new List<int> { 1 }
            };

            // Act
            var result = await _controller.CrearCursosDirigidos(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CrearCursosDirigidos_WithNonExistentEstudiante_ReturnsOkWithError()
        {
            // Arrange
            SetupAdminUser();
            await SeedDataForCursosDirigidos();

            var dto = new MatriculaDirigidaDto
            {
                IdCurso = 1,
                IdPeriodo = 1,
                IdsEstudiantes = new List<int> { 999 } // Estudiante no existe
            };

            // Act
            var result = await _controller.CrearCursosDirigidos(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
