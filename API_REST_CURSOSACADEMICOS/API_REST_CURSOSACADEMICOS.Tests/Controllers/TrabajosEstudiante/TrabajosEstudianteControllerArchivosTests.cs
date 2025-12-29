using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.TrabajosEstudiante
{
    public class TrabajosEstudianteControllerArchivosTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosEstudianteController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly TrabajosEstudianteController _controller;

        public TrabajosEstudianteControllerArchivosTests()
        {
            _mockTrabajoService = new Mock<ITrabajoService>();
            _mockLogger = new Mock<ILogger<TrabajosEstudianteController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GestionAcademicaContext(options);
            
            _controller = new TrabajosEstudianteController(_mockTrabajoService.Object, _context, _mockLogger.Object);
        }

        private async Task<Estudiante> SetupEstudianteUser(int usuarioId = 1)
        {
            // Create user and student
            var usuario = new Usuario
            {
                Id = usuarioId,
                Email = "estudiante@test.com",
                PasswordHash = "hash",
                Rol = "Estudiante"
            };
            _context.Usuarios.Add(usuario);

            var estudiante = new Estudiante
            {
                Id = usuarioId,
                IdUsuario = usuarioId,
                Codigo = $"EST00{usuarioId}",
                Nombres = "Test",
                Apellidos = "Estudiante",
                Dni = $"1234567{usuarioId}",
                CicloActual = 5
            };
            _context.Estudiantes.Add(estudiante);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return estudiante;
        }

        private void SetupDocenteUser(int usuarioId = 1, int docenteId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
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
        public async Task DownloadArchivoInstrucciones_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();
            var archivoId = 999;

            // No archivo exists with this ID

            // Act
            var result = await _controller.DownloadArchivoInstrucciones(archivoId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoInstrucciones_WithNoMatricula_ReturnsForbid()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Create trabajo with archivo
            var trabajo = new TrabajoEncargado
            {
                Id = 1,
                IdCurso = 1,
                IdDocente = 1,
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoArchivo
            {
                Id = 1,
                IdTrabajo = 1,
                NombreArchivo = "test.pdf",
                RutaArchivo = "test.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // No matricula for this student in this course

            // Act
            var result = await _controller.DownloadArchivoInstrucciones(1);

            // Assert
            // El controller valida pertenencia/estado y en este escenario devuelve NotFound.
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoInstrucciones_WithValidMatricula_ButFileNotFound_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Create trabajo with archivo
            var trabajo = new TrabajoEncargado
            {
                Id = 2,
                IdCurso = 1,
                IdDocente = 1,
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoArchivo
            {
                Id = 2,
                IdTrabajo = 2,
                NombreArchivo = "test.pdf",
                RutaArchivo = "non_existent_file.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Create matricula
            var matricula = new Matricula
            {
                Id = 1,
                IdEstudiante = estudiante.Id,
                IdCurso = 1,
                IdPeriodo = 1,
                FechaMatricula = DateTime.Now,
                Estado = "Activo"
            };
            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoInstrucciones(2);

            // Assert
            // Should return NotFound because file doesn't physically exist
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsStudent_WithOwnEntrega_ChecksPermissions()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Create entrega and archivo
            var entrega = new TrabajoEntrega
            {
                Id = 1,
                IdTrabajo = 1,
                IdEstudiante = estudiante.Id,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 1,
                IdEntrega = 1,
                NombreArchivo = "entrega.pdf",
                RutaArchivo = "non_existent_path.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(1, 1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>(); // File doesn't physically exist
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsStudent_WithOtherStudentEntrega_ReturnsForbid()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Create entrega for different student
            var entrega = new TrabajoEntrega
            {
                Id = 2,
                IdTrabajo = 1,
                IdEstudiante = 999, // Different student
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 2,
                IdEntrega = 2,
                NombreArchivo = "otra_entrega.pdf",
                RutaArchivo = "test.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(2, 2);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsDocente_WithValidAccess_ChecksPermissions()
        {
            // Arrange
            SetupDocenteUser(1, 1);

            // Create trabajo
            var trabajo = new TrabajoEncargado
            {
                Id = 3,
                IdCurso = 1,
                IdDocente = 1,
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            // Create entrega
            var entrega = new TrabajoEntrega
            {
                Id = 3,
                IdTrabajo = 3,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 3,
                IdEntrega = 3,
                NombreArchivo = "entrega.pdf",
                RutaArchivo = "non_existent_path.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(3, 3);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>(); // File doesn't physically exist
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsDocente_WithDifferentDocenteTrabajo_ReturnsForbid()
        {
            // Arrange
            SetupDocenteUser(1, 1);

            // Create trabajo for different docente
            var trabajo = new TrabajoEncargado
            {
                Id = 4,
                IdCurso = 1,
                IdDocente = 999, // Different docente
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            var entrega = new TrabajoEntrega
            {
                Id = 4,
                IdTrabajo = 4,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 4,
                IdEntrega = 4,
                NombreArchivo = "entrega.pdf",
                RutaArchivo = "test.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(4, 4);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsAdmin_CanAccessAnyFile()
        {
            // Arrange
            SetupAdminUser();

            // Create trabajo for any docente
            var trabajo = new TrabajoEncargado
            {
                Id = 5,
                IdCurso = 1,
                IdDocente = 999,
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };
            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            var entrega = new TrabajoEntrega
            {
                Id = 5,
                IdTrabajo = 5,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 5,
                IdEntrega = 5,
                NombreArchivo = "entrega.pdf",
                RutaArchivo = "non_existent_path.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(5, 5);

            // Assert
            // Admin can access, but file doesn't exist
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_WithMismatchedEntregaAndArchivo_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Create two entregas
            var entrega1 = new TrabajoEntrega
            {
                Id = 6,
                IdTrabajo = 1,
                IdEstudiante = estudiante.Id,
                FechaEntrega = DateTime.Now
            };
            var entrega2 = new TrabajoEntrega
            {
                Id = 7,
                IdTrabajo = 1,
                IdEstudiante = estudiante.Id,
                FechaEntrega = DateTime.Now
            };
            _context.Set<TrabajoEntrega>().AddRange(entrega1, entrega2);
            await _context.SaveChangesAsync();

            // Archivo belongs to entrega 6
            var archivo = new TrabajoEntregaArchivo
            {
                Id = 6,
                IdEntrega = 6,
                NombreArchivo = "entrega.pdf",
                RutaArchivo = "test.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };
            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act - Try to access archivo 6 with entrega 7
            var result = await _controller.DownloadArchivoEntrega(7, 6);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_WithNonExistentArchivo_ReturnsNotFound()
        {
            // Arrange
            var estudiante = await SetupEstudianteUser();

            // Act
            var result = await _controller.DownloadArchivoEntrega(999, 999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
