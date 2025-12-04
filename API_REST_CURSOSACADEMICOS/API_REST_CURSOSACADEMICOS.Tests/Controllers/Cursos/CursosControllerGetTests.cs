using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Cursos
{
    public class CursosControllerGetTests
    {
        private readonly GestionAcademicaContext _context;
        private readonly CursosController _controller;

        public CursosControllerGetTests()
        {
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new CursosController(_context);
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

        private async Task SeedCursos()
        {
            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "Pérez",
                Profesion = "Ingeniero"
            };
            await _context.Docentes.AddAsync(docente);

            var cursos = new List<Curso>
            {
                new Curso
                {
                    Id = 1,
                    Codigo = "MAT101",
                    NombreCurso = "Matemáticas I",
                    Creditos = 4,
                    HorasSemanal = 6,
                    Ciclo = 1,
                    IdDocente = 1
                },
                new Curso
                {
                    Id = 2,
                    Codigo = "FIS101",
                    NombreCurso = "Física I",
                    Creditos = 4,
                    HorasSemanal = 6,
                    Ciclo = 1,
                    IdDocente = 1
                },
                new Curso
                {
                    Id = 3,
                    Codigo = "MAT201",
                    NombreCurso = "Matemáticas II",
                    Creditos = 4,
                    HorasSemanal = 6,
                    Ciclo = 2,
                    IdDocente = 1
                }
            };

            await _context.Cursos.AddRangeAsync(cursos);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetCursos_ReturnsAllCursos()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCursos();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<IEnumerable<CursoDto>>().Subject;
            cursos.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetCurso_WithValidId_ReturnsCurso()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCurso(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var curso = okResult.Value.Should().BeOfType<CursoDto>().Subject;
            curso.Codigo.Should().Be("MAT101");
            curso.NombreCurso.Should().Be("Matemáticas I");
        }

        [Fact]
        public async Task GetCurso_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCurso(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetCursosPorDocente_ReturnsDocenteCursos()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCursosPorDocente(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<IEnumerable<CursoDto>>().Subject;
            cursos.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetCursosPorDocente_WithInvalidDocenteId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.GetCursosPorDocente(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetCursosPorCiclo_ReturnsCiclosCursos()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCursosPorCiclo(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<IEnumerable<CursoDto>>().Subject;
            cursos.Should().HaveCount(2); // MAT101 y FIS101 son del ciclo 1
        }

        [Fact]
        public async Task GetCursosPorCiclo_WithNoCursos_ReturnsEmptyList()
        {
            // Arrange
            SetupAdminUser();
            await SeedCursos();

            // Act
            var result = await _controller.GetCursosPorCiclo(10); // Ciclo sin cursos

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<IEnumerable<CursoDto>>().Subject;
            cursos.Should().BeEmpty();
        }
    }
}
