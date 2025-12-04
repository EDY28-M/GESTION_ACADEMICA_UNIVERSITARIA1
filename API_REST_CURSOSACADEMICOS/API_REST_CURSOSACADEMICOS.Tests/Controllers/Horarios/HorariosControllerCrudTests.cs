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

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Horarios
{
    public class HorariosControllerCrudTests
    {
        private readonly Mock<IHorarioService> _mockHorarioService;
        private readonly GestionAcademicaContext _context;
        private readonly HorariosController _controller;

        public HorariosControllerCrudTests()
        {
            _mockHorarioService = new Mock<IHorarioService>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new HorariosController(_mockHorarioService.Object, _context);
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

        [Fact]
        public async Task CrearHorario_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupAdminUser();

            var crearHorarioDto = new CrearHorarioDto
            {
                IdCurso = 1,
                DiaSemana = 1,
                HoraInicio = "08:00",
                HoraFin = "10:00",
                Aula = "A101",
                Tipo = "Teoría"
            };

            var horarioDto = new HorarioDto
            {
                Id = 1,
                IdCurso = 1,
                NombreCurso = "Matemáticas",
                DiaSemana = 1,
                DiaSemanaTexto = "Lunes",
                HoraInicio = "08:00",
                HoraFin = "10:00",
                Aula = "A101",
                Tipo = "Teoría"
            };

            _mockHorarioService.Setup(s => s.CrearHorarioAsync(It.IsAny<CrearHorarioDto>()))
                .ReturnsAsync(horarioDto);

            // Act
            var result = await _controller.CrearHorario(crearHorarioDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var horario = createdResult.Value.Should().BeOfType<HorarioDto>().Subject;
            horario.Aula.Should().Be("A101");
        }

        [Fact]
        public async Task CrearHorario_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var crearHorarioDto = new CrearHorarioDto
            {
                IdCurso = 1,
                DiaSemana = 1,
                HoraInicio = "08:00",
                HoraFin = "10:00",
                Tipo = "Teoría"
            };

            _mockHorarioService.Setup(s => s.CrearHorarioAsync(It.IsAny<CrearHorarioDto>()))
                .ThrowsAsync(new ArgumentException("Datos inválidos"));

            // Act
            var result = await _controller.CrearHorario(crearHorarioDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearHorario_WithConflict_ReturnsConflict()
        {
            // Arrange
            SetupAdminUser();

            var crearHorarioDto = new CrearHorarioDto
            {
                IdCurso = 1,
                DiaSemana = 1,
                HoraInicio = "08:00",
                HoraFin = "10:00",
                Tipo = "Teoría"
            };

            _mockHorarioService.Setup(s => s.CrearHorarioAsync(It.IsAny<CrearHorarioDto>()))
                .ThrowsAsync(new InvalidOperationException("Conflicto de horario"));

            // Act
            var result = await _controller.CrearHorario(crearHorarioDto);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task EliminarHorario_WithValidId_ReturnsNoContent()
        {
            // Arrange
            SetupAdminUser();

            _mockHorarioService.Setup(s => s.EliminarHorarioAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.EliminarHorario(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task EliminarHorario_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            _mockHorarioService.Setup(s => s.EliminarHorarioAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.EliminarHorario(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetHorariosPorCurso_ReturnsHorarios()
        {
            // Arrange
            SetupAdminUser();

            var horarios = new List<HorarioDto>
            {
                new HorarioDto { Id = 1, IdCurso = 1, DiaSemanaTexto = "Lunes" },
                new HorarioDto { Id = 2, IdCurso = 1, DiaSemanaTexto = "Miércoles" }
            };

            _mockHorarioService.Setup(s => s.ObtenerPorCursoAsync(1)).ReturnsAsync(horarios);

            // Act
            var result = await _controller.GetHorariosPorCurso(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedHorarios = okResult.Value.Should().BeAssignableTo<IEnumerable<HorarioDto>>().Subject;
            returnedHorarios.Should().HaveCount(2);
        }
    }
}
