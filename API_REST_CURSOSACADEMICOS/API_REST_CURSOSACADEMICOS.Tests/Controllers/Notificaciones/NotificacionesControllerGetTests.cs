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
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Notificaciones
{
    public class NotificacionesControllerGetTests
    {
        private readonly Mock<IHubContext<NotificationsHub>> _mockHubContext;
        private readonly GestionAcademicaContext _context;
        private readonly NotificacionesController _controller;

        public NotificacionesControllerGetTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationsHub>>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _controller = new NotificacionesController(_context, _mockHubContext.Object);
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
                    Mensaje = "Matrícula realizada",
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
                    Leida = true
                },
                new Notificacion
                {
                    Id = 3,
                    Tipo = "global",
                    Accion = "anuncio",
                    Mensaje = "Anuncio global",
                    IdUsuario = null, // Notificación global
                    FechaCreacion = DateTime.Now.AddHours(-2),
                    Leida = false
                },
                new Notificacion
                {
                    Id = 4,
                    Tipo = "info",
                    Accion = "otro",
                    Mensaje = "Notificación de otro usuario",
                    IdUsuario = 2,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                }
            };

            await _context.Notificaciones.AddRangeAsync(notificaciones);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetNotificaciones_ReturnsUserAndGlobalNotifications()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            // Act
            var result = await _controller.GetNotificaciones(100);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notificaciones = okResult.Value.Should().BeAssignableTo<List<NotificacionDto>>().Subject;
            // Debería devolver 3: 2 del usuario 1 + 1 global
            notificaciones.Should().HaveCount(3);
            notificaciones.Should().Contain(n => n.Tipo == "global"); // Global notification
            notificaciones.Should().NotContain(n => n.Mensaje == "Notificación de otro usuario");
        }

        [Fact]
        public async Task GetNotificaciones_RespectsLimit()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            // Act
            var result = await _controller.GetNotificaciones(2);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var notificaciones = okResult.Value.Should().BeAssignableTo<List<NotificacionDto>>().Subject;
            notificaciones.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetNotificaciones_WithNoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            await SeedNotificaciones();
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetNotificaciones(100);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetCountNoLeidas_ReturnsCorrectCount()
        {
            // Arrange
            await SeedNotificaciones();
            SetupUser(1);

            // Act
            var result = await _controller.GetCountNoLeidas();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            // El usuario 1 tiene 1 no leída + 1 global no leída = 2
        }

        [Fact]
        public async Task GetCountNoLeidas_WithNoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetCountNoLeidas();

            // Assert
            result.Result.Should().BeOfType<UnauthorizedResult>();
        }
    }
}
