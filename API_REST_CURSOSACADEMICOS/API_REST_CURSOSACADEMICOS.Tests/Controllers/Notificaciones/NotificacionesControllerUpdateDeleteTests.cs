using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Hubs;
using API_REST_CURSOSACADEMICOS.Services;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Notificaciones
{
    public class NotificacionesControllerUpdateDeleteTests
    {
        private readonly Mock<IHubContext<NotificationsHub>> _mockHubContext;
        private readonly GestionAcademicaContext _context;
        private readonly NotificacionesController _controller;

        public NotificacionesControllerUpdateDeleteTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationsHub>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new NotificacionesController(new NotificacionesService(_context), _mockHubContext.Object);
        }

        private void SetupUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private async Task SeedNotificaciones()
        {
            var notificaciones = new List<Notificacion>
            {
                new Notificacion
                {
                    Id = 1,
                    Tipo = "info",
                    Accion = "matricula",
                    Mensaje = "Matr�cula realizada",
                    IdUsuario = 1,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                },
                new Notificacion
                {
                    Id = 2,
                    Tipo = "warning",
                    Accion = "nota",
                    Mensaje = "Nueva nota registrada",
                    IdUsuario = 1,
                    FechaCreacion = DateTime.Now.AddHours(-1),
                    Leida = false
                },
                new Notificacion
                {
                    Id = 3,
                    Tipo = "global",
                    Accion = "anuncio",
                    Mensaje = "Anuncio global",
                    IdUsuario = null,
                    FechaCreacion = DateTime.Now.AddHours(-2),
                    Leida = false
                },
                new Notificacion
                {
                    Id = 4,
                    Tipo = "info",
                    Accion = "otro",
                    Mensaje = "Notificaci�n de otro usuario",
                    IdUsuario = 2,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                }
            };

            await _context.Notificaciones.AddRangeAsync(notificaciones);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task MarcarComoLeidas_WithValidIds_MarksAsRead()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            var markReadDto = new NotificacionMarkReadDto
            {
                NotificacionIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _controller.MarcarComoLeidas(markReadDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var notificacion1 = await _context.Notificaciones.FindAsync(1);
            var notificacion2 = await _context.Notificaciones.FindAsync(2);
            
            notificacion1!.Leida.Should().BeTrue();
            notificacion2!.Leida.Should().BeTrue();
        }

        [Fact]
        public async Task MarcarComoLeidas_OnlyMarksUserNotifications()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            var markReadDto = new NotificacionMarkReadDto
            {
                NotificacionIds = new List<int> { 1, 4 } // 4 es de otro usuario
            };

            // Act
            var result = await _controller.MarcarComoLeidas(markReadDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var notificacion1 = await _context.Notificaciones.FindAsync(1);
            var notificacion4 = await _context.Notificaciones.FindAsync(4);
            
            notificacion1!.Leida.Should().BeTrue();
            notificacion4!.Leida.Should().BeFalse(); // No deber�a cambiar
        }

        [Fact]
        public async Task MarcarComoLeidas_CanMarkGlobalNotifications()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            var markReadDto = new NotificacionMarkReadDto
            {
                NotificacionIds = new List<int> { 3 } // Global notification
            };

            // Act
            var result = await _controller.MarcarComoLeidas(markReadDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            var notificacion3 = await _context.Notificaciones.FindAsync(3);
            notificacion3!.Leida.Should().BeTrue();
        }

        [Fact]
        public async Task MarcarComoLeidas_WithNoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            await SeedNotificaciones();
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var markReadDto = new NotificacionMarkReadDto
            {
                NotificacionIds = new List<int> { 1 }
            };

            // Act
            var result = await _controller.MarcarComoLeidas(markReadDto);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task LimpiarNotificaciones_DeletesUserAndGlobalNotifications()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            // Act
            var result = await _controller.LimpiarNotificaciones();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            // Las del usuario 1 y las globales deben ser eliminadas
            var remainingNotifications = await _context.Notificaciones.ToListAsync();
            remainingNotifications.Should().HaveCount(1); // Solo la del usuario 2
            remainingNotifications[0].IdUsuario.Should().Be(2);
        }

        [Fact]
        public async Task LimpiarNotificaciones_WithNoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            await SeedNotificaciones();
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.LimpiarNotificaciones();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task LimpiarNotificaciones_WithNoNotifications_ReturnsOkWithZeroCount()
        {
            // Arrange
            SetupUser(1);
            // No seeding - empty database

            // Act
            var result = await _controller.LimpiarNotificaciones();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
