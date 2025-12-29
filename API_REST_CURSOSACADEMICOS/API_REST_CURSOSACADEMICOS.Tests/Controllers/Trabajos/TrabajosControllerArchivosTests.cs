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

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Trabajos
{
    public class TrabajosControllerArchivosTests
    {
        private readonly Mock<ITrabajoService> _mockTrabajoService;
        private readonly Mock<ILogger<TrabajosController>> _mockLogger;
        private readonly GestionAcademicaContext _context;
        private readonly TrabajosController _controller;

        public TrabajosControllerArchivosTests()
        {
            _mockTrabajoService = new Mock<ITrabajoService>();
            _mockLogger = new Mock<ILogger<TrabajosController>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GestionAcademicaContext(options);
            
            _controller = new TrabajosController(_mockTrabajoService.Object, _context, _mockLogger.Object);
        }

        private void SetupDocenteUser(int docenteId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, docenteId.ToString()),
                new Claim(ClaimTypes.Role, "Docente"),
                new Claim("DocenteId", docenteId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
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

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SetupEstudianteUser(int usuarioId = 1)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Role, "Estudiante")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task DownloadArchivoInstrucciones_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var archivoId = 999;

            // No archivo exists with this ID in the database

            // Act
            var result = await _controller.DownloadArchivoInstrucciones(archivoId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();
            var entregaId = 999;
            var archivoId = 999;

            // No archivo exists with this ID in the database

            // Act
            var result = await _controller.DownloadArchivoEntrega(entregaId, archivoId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsDocente_WithValidId_ChecksPermissions()
        {
            // Arrange
            SetupDocenteUser();

            // Create test data in the in-memory database
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

            var entrega = new TrabajoEntrega
            {
                Id = 1,
                IdTrabajo = 1,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };

            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            // Archivo without physical file
            var archivo = new TrabajoEntregaArchivo
            {
                Id = 1,
                IdEntrega = 1,
                NombreArchivo = "test.pdf",
                RutaArchivo = "non_existent_path.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };

            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(1, 1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>(); // File doesn't exist
        }

        [Fact]
        public async Task DownloadArchivoEntrega_AsAdmin_CanAccessAnyFile()
        {
            // Arrange
            SetupAdminUser();

            // Create test data
            var trabajo = new TrabajoEncargado
            {
                Id = 2,
                IdCurso = 1,
                IdDocente = 2, // Different docente
                Titulo = "Test Trabajo",
                FechaLimite = DateTime.Now.AddDays(7),
                Activo = true
            };

            _context.Set<TrabajoEncargado>().Add(trabajo);
            await _context.SaveChangesAsync();

            var entrega = new TrabajoEntrega
            {
                Id = 2,
                IdTrabajo = 2,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };

            _context.Set<TrabajoEntrega>().Add(entrega);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 2,
                IdEntrega = 2,
                NombreArchivo = "test.pdf",
                RutaArchivo = "non_existent_path.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };

            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DownloadArchivoEntrega(2, 2);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>(); // File doesn't physically exist
        }

        [Fact]
        public async Task DownloadArchivoEntrega_WithMismatchedIds_ReturnsNotFound()
        {
            // Arrange
            SetupDocenteUser();

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

            var entrega1 = new TrabajoEntrega
            {
                Id = 3,
                IdTrabajo = 3,
                IdEstudiante = 1,
                FechaEntrega = DateTime.Now
            };

            var entrega2 = new TrabajoEntrega
            {
                Id = 4,
                IdTrabajo = 3,
                IdEstudiante = 2,
                FechaEntrega = DateTime.Now
            };

            _context.Set<TrabajoEntrega>().AddRange(entrega1, entrega2);
            await _context.SaveChangesAsync();

            var archivo = new TrabajoEntregaArchivo
            {
                Id = 3,
                IdEntrega = 3, // Belongs to entrega 3
                NombreArchivo = "test.pdf",
                RutaArchivo = "test.pdf",
                TipoArchivo = "application/pdf",
                Tamaño = 1024
            };

            _context.Set<TrabajoEntregaArchivo>().Add(archivo);
            await _context.SaveChangesAsync();

            // Act - Try to access archivo 3 with entrega 4
            var result = await _controller.DownloadArchivoEntrega(4, 3);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
