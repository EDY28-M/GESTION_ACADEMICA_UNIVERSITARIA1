using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Docentes
{
    public class DocentesControllerCursosTests
    {
        private readonly Mock<ILogger<DocentesController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly DocentesController _controller;

        public DocentesControllerCursosTests()
        {
            _mockLogger = new Mock<ILogger<DocentesController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            var asistenciaServiceMock = new Mock<IAsistenciaService>();
            var docentesService = new DocentesService(_context, asistenciaServiceMock.Object);
            _controller = new DocentesController(docentesService, _mockLogger.Object);
        }

        private void SetupDocenteUser(int docenteId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, docenteId.ToString()),
                new Claim(ClaimTypes.Role, "Docente"),
                new Claim("DocenteId", docenteId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private void SetupOtherRoleUser()
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

        private async Task SeedData()
        {
            var periodo = new Periodo
            {
                Id = 1,
                Nombre = "2024-I",
                Activo = true,
                FechaInicio = DateTime.Now.AddMonths(-2),
                FechaFin = DateTime.Now.AddMonths(2)
            };
            await _context.Periodos.AddAsync(periodo);

            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "P�rez",
                Profesion = "Ingeniero",
                Correo = "juan@test.com"
            };
            await _context.Docentes.AddAsync(docente);

            var curso = new Curso
            {
                Id = 1,
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas I",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 1
            };
            await _context.Cursos.AddAsync(curso);

            var estudiante = new Estudiante
            {
                Id = 1,
                Codigo = "EST001",
                Nombres = "Mar�a",
                Apellidos = "Garc�a",
                CicloActual = 1
            };
            await _context.Estudiantes.AddAsync(estudiante);

            var matricula = new Matricula
            {
                Id = 1,
                IdEstudiante = 1,
                IdCurso = 1,
                IdPeriodo = 1,
                Estado = "Matriculado",
                FechaMatricula = DateTime.Now.AddDays(-30)
            };
            await _context.Matriculas.AddAsync(matricula);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetMisCursos_AsDocente_ReturnsCursos()
        {
            // Arrange
            await SeedData();
            SetupDocenteUser(1);

            // Act
            var result = await _controller.GetMisCursos();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<List<CursoDocenteDto>>().Subject;
            cursos.Should().HaveCount(1);
            cursos[0].NombreCurso.Should().Be("Matem�ticas I");
        }

        [Fact]
        public async Task GetMisCursos_NotAsDocente_ReturnsForbid()
        {
            // Arrange
            await SeedData();
            SetupOtherRoleUser();

            // Act
            var result = await _controller.GetMisCursos();

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetMisCursos_WithNoDocenteId_ReturnsUnauthorized()
        {
            // Arrange
            await SeedData();
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Docente")
                // Sin DocenteId claim
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetMisCursos();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetEstudiantesCurso_WithValidCurso_ReturnsEstudiantes()
        {
            // Arrange
            await SeedData();
            SetupDocenteUser(1);

            // Act
            var result = await _controller.GetEstudiantesCurso(1);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var estudiantes = okResult.Value.Should().BeAssignableTo<List<EstudianteCursoDto>>().Subject;
            estudiantes.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetEstudiantesCurso_CursoNotFound_ReturnsNotFound()
        {
            // Arrange
            await SeedData();
            SetupDocenteUser(1);

            // Act
            var result = await _controller.GetEstudiantesCurso(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetEstudiantesCurso_CursoNotOwnedByDocente_ReturnsForbid()
        {
            // Arrange
            await SeedData();
            
            // Agregar otro curso de otro docente
            _context.Cursos.Add(new Curso
            {
                Id = 2,
                NombreCurso = "F�sica",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 999 // Otro docente
            });
            await _context.SaveChangesAsync();

            SetupDocenteUser(1); // Docente 1 intenta acceder a curso del docente 999

            // Act
            var result = await _controller.GetEstudiantesCurso(2);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
    }
}
