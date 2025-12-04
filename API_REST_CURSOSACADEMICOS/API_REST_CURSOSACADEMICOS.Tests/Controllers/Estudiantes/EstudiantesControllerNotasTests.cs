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
    public class EstudiantesControllerNotasTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly GestionAcademicaContext _context;
        private readonly EstudiantesController _controller;

        public EstudiantesControllerNotasTests()
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
        public async Task GetNotas_ReturnsNotasWithStatistics()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto 
            { 
                Id = 1, 
                PromedioAcumulado = 15.5m,
                PromedioSemestral = 16.0m,
                CreditosAcumulados = 45
            };

            var notas = new List<NotaDto>
            {
                new NotaDto { Id = 1, TipoEvaluacion = "Parcial 1", NotaValor = 15 }
            };

            var matriculas = new List<MatriculaDto>
            {
                new MatriculaDto { Id = 1, Estado = "Matriculado", PromedioFinal = 15 }
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetNotasAsync(1, null)).ReturnsAsync(notas);
            _mockEstudianteService.Setup(s => s.GetMisCursosAsync(1, null)).ReturnsAsync(matriculas);

            // Act
            var result = await _controller.GetNotas(null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetNotas_StudentNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(1);

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync((EstudianteDto?)null);

            // Act
            var result = await _controller.GetNotas(null);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetNotasConEstadisticas_ReturnsDetailedStats()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto 
            { 
                Id = 1, 
                PromedioAcumulado = 15.5m,
                PromedioSemestral = 16.0m,
                CreditosAcumulados = 45
            };

            var notas = new List<NotaDto>
            {
                new NotaDto { Id = 1, TipoEvaluacion = "Parcial 1", NotaValor = 15 }
            };

            var matriculas = new List<MatriculaDto>
            {
                new MatriculaDto { Id = 1, Estado = "Matriculado", PromedioFinal = 15 }
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetNotasAsync(1, null)).ReturnsAsync(notas);
            _mockEstudianteService.Setup(s => s.GetMisCursosAsync(1, null)).ReturnsAsync(matriculas);

            // Act
            var result = await _controller.GetNotasConEstadisticas(null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetEstadisticas_ReturnsCompleteStatistics()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto { Id = 1 };
            var estudiante = new Estudiante 
            { 
                Id = 1, 
                CreditosAcumulados = 45, 
                PromedioAcumulado = 15.5m,
                PromedioSemestral = 16.0m,
                IdUsuario = 1
            };
            await _context.Estudiantes.AddAsync(estudiante);
            await _context.SaveChangesAsync();

            var matriculas = new List<MatriculaDto>
            {
                new MatriculaDto { Id = 1, Estado = "Matriculado", PromedioFinal = 15 },
                new MatriculaDto { Id = 2, Estado = "Aprobado", PromedioFinal = 16 },
                new MatriculaDto { Id = 3, Estado = "Retirado" }
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1)).ReturnsAsync(estudianteDto);
            _mockEstudianteService.Setup(s => s.GetMisCursosAsync(1, null)).ReturnsAsync(matriculas);

            // Act
            var result = await _controller.GetEstadisticas(null);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetPeriodos_ReturnsAllPeriodos()
        {
            // Arrange
            SetupEstudianteUser(1);

            var periodos = new List<PeriodoDto>
            {
                new PeriodoDto { Id = 1, Nombre = "2024-I" },
                new PeriodoDto { Id = 2, Nombre = "2023-II" }
            };

            _mockEstudianteService.Setup(s => s.GetPeriodosAsync()).ReturnsAsync(periodos);

            // Act
            var result = await _controller.GetPeriodos();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedPeriodos = okResult.Value.Should().BeAssignableTo<List<PeriodoDto>>().Subject;
            returnedPeriodos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPeriodoActivo_ReturnsPeriodo()
        {
            // Arrange
            SetupEstudianteUser(1);

            var periodoActivo = new PeriodoDto { Id = 1, Nombre = "2024-I", Activo = true };

            _mockEstudianteService.Setup(s => s.GetPeriodoActivoAsync()).ReturnsAsync(periodoActivo);

            // Act
            var result = await _controller.GetPeriodoActivo();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var periodo = okResult.Value.Should().BeOfType<PeriodoDto>().Subject;
            periodo.Nombre.Should().Be("2024-I");
        }

        [Fact]
        public async Task GetPeriodoActivo_WhenNoPeriodoActivo_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(1);

            _mockEstudianteService.Setup(s => s.GetPeriodoActivoAsync()).ReturnsAsync((PeriodoDto?)null);

            // Act
            var result = await _controller.GetPeriodoActivo();

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
