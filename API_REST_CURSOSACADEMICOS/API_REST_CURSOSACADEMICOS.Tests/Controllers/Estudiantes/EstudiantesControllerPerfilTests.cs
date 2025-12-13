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

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Estudiantes
{
    public class EstudiantesControllerPerfilTests
    {
        private readonly Mock<IEstudianteService> _mockEstudianteService;
        private readonly GestionAcademicaContext _context;
        private readonly EstudiantesController _controller;

        public EstudiantesControllerPerfilTests()
        {
            _mockEstudianteService = new Mock<IEstudianteService>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            var controllerService = new EstudiantesControllerService(_context, _mockEstudianteService.Object);
            _controller = new EstudiantesController(_mockEstudianteService.Object, controllerService);
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
        public async Task GetPerfil_WithValidUser_ReturnsPerfil()
        {
            // Arrange
            SetupEstudianteUser(1);

            var estudianteDto = new EstudianteDto
            {
                Id = 1,
                Codigo = "EST001",
                Nombres = "Juan",
                Apellidos = "P�rez",
                CicloActual = 3
            };

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1))
                .ReturnsAsync(estudianteDto);

            // Act
            var result = await _controller.GetPerfil();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var estudiante = okResult.Value.Should().BeOfType<EstudianteDto>().Subject;
            estudiante.Codigo.Should().Be("EST001");
        }

        [Fact]
        public async Task GetPerfil_PerfilNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupEstudianteUser(1);

            _mockEstudianteService.Setup(s => s.GetByUsuarioIdAsync(1))
                .ReturnsAsync((EstudianteDto?)null);

            // Act
            var result = await _controller.GetPerfil();

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetPerfil_WithNoAuthentication_ThrowsException()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act & Assert
            var result = await _controller.GetPerfil();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAll_AsAdmin_ReturnsAllEstudiantes()
        {
            // Arrange
            SetupAdminUser();

            var estudiante = new Estudiante
            {
                Id = 1,
                Codigo = "EST001",
                Nombres = "Juan",
                Apellidos = "P�rez",
                CicloActual = 3,
                Estado = "Activo"
            };
            await _context.Estudiantes.AddAsync(estudiante);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var estudiantes = okResult.Value.Should().BeAssignableTo<List<EstudianteDto>>().Subject;
            estudiantes.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsEstudiante()
        {
            // Arrange
            SetupAdminUser();

            var estudiante = new Estudiante
            {
                Id = 1,
                Codigo = "EST001",
                Nombres = "Juan",
                Apellidos = "P�rez",
                CicloActual = 3
            };
            await _context.Estudiantes.AddAsync(estudiante);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetById(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var estudianteDto = okResult.Value.Should().BeOfType<EstudianteDto>().Subject;
            estudianteDto.Codigo.Should().Be("EST001");
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupAdminUser();

            // Act
            var result = await _controller.GetById(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
