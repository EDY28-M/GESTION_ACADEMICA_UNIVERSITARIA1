using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Estudiantes
{
    public class EstudiantesControllerMatriculaTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly GestionAcademicaContext _context;
        private readonly EstudiantesController _controller;

        public EstudiantesControllerMatriculaTests()
        {
            _mockEstudianteService = new Mock<IEstudianteService>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new EstudiantesController(_mockEstudianteService.Object, _context);
        }

        private void SetupEstudianteUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetCursosDisponibles_ReturnsAvailableCourses()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1, CicloActual = 3 };
            var periodoDto = new PeriodoDto { Id = 1, Nombre = "2024-I" };
            var cursosDisponibles = new List<CursoDisponibleDto>
            {
                new CursoDisponibleDto { Id = 1, Codigo = "MAT101", NombreCurso = "Matemáticas" }
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetPeriodoActivoAsync()).ReturnsAsync(periodoDto);
            _mockEstudianteService.Setup(s => s.GetCursosDisponiblesPorEstudianteAsync(1)).ReturnsAsync(cursosDisponibles);

            // Act
            var result = await _controller.GetCursosDisponibles(null);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<List<CursoDisponibleDto>>().Subject;
            cursos.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCursosDisponibles_WithNoPeriodoActivo_ReturnsBadRequest()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1, CicloActual = 3 };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetPeriodoActivoAsync()).ReturnsAsync((PeriodoDto?)null);

            // Act
            var result = await _controller.GetCursosDisponibles(null);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMisCursos_ReturnsMyCourses()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1 };
            var matriculas = new List<MatriculaDto>
            {
                new MatriculaDto { Id = 1, NombreCurso = "Matemáticas", Estado = "Matriculado" }
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetMisCursosAsync(1, null)).ReturnsAsync(matriculas);

            // Act
            var result = await _controller.GetMisCursos(null);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var cursos = okResult.Value.Should().BeAssignableTo<List<MatriculaDto>>().Subject;
            cursos.Should().HaveCount(1);
        }

        [Fact]
        public async Task Matricular_WithValidData_ReturnsMatricula()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1 };
            var matricularDto = new MatricularDto { IdCurso = 1, IdPeriodo = 1 };
            var matriculaDto = new MatriculaDto { Id = 1, IdCurso = 1, Estado = "Matriculado" };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.MatricularAsync(1, It.IsAny<MatricularDto>(), false))
                .ReturnsAsync(matriculaDto);

            // Act
            var result = await _controller.Matricular(matricularDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var matricula = okResult.Value.Should().BeOfType<MatriculaDto>().Subject;
            matricula.Estado.Should().Be("Matriculado");
        }

        [Fact]
        public async Task Matricular_StudentNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(1);

            var matricularDto = new MatricularDto { IdCurso = 1, IdPeriodo = 1 };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync((EstudianteDto?)null);

            // Act
            var result = await _controller.Matricular(matricularDto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Retirar_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1 };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.RetirarAsync(1, 1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Retirar(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Retirar_StudentNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(1);

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync((EstudianteDto?)null);

            // Act
            var result = await _controller.Retirar(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Retirar_WhenServiceThrows_ReturnsBadRequest()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1 };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.RetirarAsync(1, 1)).ThrowsAsync(new Exception("Error al retirar"));

            // Act
            var result = await _controller.Retirar(1);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
