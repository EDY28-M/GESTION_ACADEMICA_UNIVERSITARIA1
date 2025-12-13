using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Cursos
{
    public class CursosControllerCrudTests
    {
        private readonly GestionAcademicaContext _context;
        private readonly CursosController _controller;

        public CursosControllerCrudTests()
        {
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new CursosController(new CursosService(_context));
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

        private async Task SeedDocente()
        {
            var docente = new Docente
            {
                Id = 1,
                Nombres = "Juan",
                Apellidos = "P�rez",
                Profesion = "Ingeniero"
            };
            await _context.Docentes.AddAsync(docente);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task PostCurso_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocente();

            var cursoDto = new CursoCreateDto
            {
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas I",
                Creditos = 4,
                HorasSemanal = 6,
                HorasTeoria = 4,
                HorasPractica = 2,
                Ciclo = 1,
                IdDocente = 1,
                PrerequisitosIds = new List<int>()
            };

            // Act
            var result = await _controller.PostCurso(cursoDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var curso = createdResult.Value.Should().BeOfType<CursoDto>().Subject;
            curso.Codigo.Should().Be("MAT101");
            curso.NombreCurso.Should().Be("Matem�ticas I");
        }

        [Fact]
        public async Task PostCurso_WithInvalidDocente_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var cursoDto = new CursoCreateDto
            {
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas I",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 999, // Docente inexistente
                PrerequisitosIds = new List<int>()
            };

            // Act
            var result = await _controller.PostCurso(cursoDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PostCurso_WithInvalidPrerequisito_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocente();

            var cursoDto = new CursoCreateDto
            {
                Codigo = "MAT201",
                NombreCurso = "Matem�ticas II",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 2,
                IdDocente = 1,
                PrerequisitosIds = new List<int> { 999 } // Prerequisito inexistente
            };

            // Act
            var result = await _controller.PostCurso(cursoDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task PostCurso_WithValidPrerequisitos_CreatesWithPrerequisitos()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocente();

            // Crear curso prerequisito primero
            var cursoPrereq = new Curso
            {
                Id = 1,
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas I",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                IdDocente = 1
            };
            await _context.Cursos.AddAsync(cursoPrereq);
            await _context.SaveChangesAsync();

            var cursoDto = new CursoCreateDto
            {
                Codigo = "MAT201",
                NombreCurso = "Matem�ticas II",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 2,
                IdDocente = 1,
                PrerequisitosIds = new List<int> { 1 }
            };

            // Act
            var result = await _controller.PostCurso(cursoDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var curso = createdResult.Value.Should().BeOfType<CursoDto>().Subject;
            curso.PrerequisitosIds.Should().Contain(1);
        }

        [Fact]
        public async Task PutCurso_WithValidData_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocente();

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
            await _context.SaveChangesAsync();

            var updateDto = new CursoUpdateDto
            {
                Codigo = "MAT101",
                NombreCurso = "Matem�ticas I Actualizado",
                Creditos = 5,
                HorasSemanal = 8,
                Ciclo = 1,
                IdDocente = 1,
                PrerequisitosIds = new List<int>()
            };

            // Act
            var result = await _controller.PutCurso(1, updateDto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            
            var updatedCurso = await _context.Cursos.FindAsync(1);
            updatedCurso!.NombreCurso.Should().Be("Matem�ticas I Actualizado");
            updatedCurso.Creditos.Should().Be(5);
        }

        [Fact]
        public async Task PutCurso_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            var updateDto = new CursoUpdateDto
            {
                Codigo = "MAT101",
                NombreCurso = "Test",
                Creditos = 4,
                HorasSemanal = 6,
                Ciclo = 1,
                PrerequisitosIds = new List<int>()
            };

            // Act
            var result = await _controller.PutCurso(999, updateDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteCurso_WithValidId_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();
            await SeedDocente();

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
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteCurso(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            
            var deletedCurso = await _context.Cursos.FindAsync(1);
            deletedCurso.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCurso_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.DeleteCurso(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
