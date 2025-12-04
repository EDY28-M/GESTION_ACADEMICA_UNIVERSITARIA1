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
    public class NotificacionesControllerCreateTests
    {
        private readonly Mock<IHubContext<NotificationsHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IHubClients> _mockClients;
        private readonly GestionAcademicaContext _context;
        private readonly NotificacionesController _controller;

        public NotificacionesControllerCreateTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationsHub>>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockClients = new Mock<IHubClients>();
            
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);

            // Setup SignalR mock
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

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

        [Fact]
        public async Task CrearNotificacion_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            SetupUser(1);

            var createDto = new NotificacionCreateDto
            {
                Tipo = "info",
                Accion = "matricula",
                Mensaje = "Matrícula realizada exitosamente",
                IdUsuario = 1
            };

            // Act
            var result = await _controller.CrearNotificacion(createDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var notificacion = createdResult.Value.Should().BeOfType<NotificacionDto>().Subject;
            notificacion.Tipo.Should().Be("info");
            notificacion.Mensaje.Should().Be("Matrícula realizada exitosamente");
            notificacion.Leida.Should().BeFalse();
        }

        [Fact]
        public async Task CrearNotificacion_SendsToSpecificUser_WhenIdUsuarioProvided()
        {
            // Arrange
            SetupUser(1);

            var createDto = new NotificacionCreateDto
            {
                Tipo = "info",
                Accion = "nota",
                Mensaje = "Nueva nota registrada",
                IdUsuario = 5
            };

            // Act
            await _controller.CrearNotificacion(createDto);

            // Assert
            _mockClients.Verify(c => c.User("5"), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task CrearNotificacion_SendsToAll_WhenIdUsuarioIsNull()
        {
            // Arrange
            SetupUser(1);

            var createDto = new NotificacionCreateDto
            {
                Tipo = "global",
                Accion = "anuncio",
                Mensaje = "Anuncio global para todos",
                IdUsuario = null
            };

            // Act
            await _controller.CrearNotificacion(createDto);

            // Assert
            _mockClients.Verify(c => c.All, Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task CrearNotificacion_SavesNotificationToDatabase()
        {
            // Arrange
            SetupUser(1);

            var createDto = new NotificacionCreateDto
            {
                Tipo = "info",
                Accion = "test",
                Mensaje = "Test notification",
                IdUsuario = 1
            };

            // Act
            await _controller.CrearNotificacion(createDto);

            // Assert
            var savedNotification = await _context.Notificaciones.FirstOrDefaultAsync();
            savedNotification.Should().NotBeNull();
            savedNotification!.Mensaje.Should().Be("Test notification");
        }

        [Fact]
        public async Task CrearNotificacion_WithMetadata_SavesMetadataCorrectly()
        {
            // Arrange
            SetupUser(1);

            var createDto = new NotificacionCreateDto
            {
                Tipo = "info",
                Accion = "matricula",
                Mensaje = "Matrícula realizada",
                Metadata = new { cursoId = 1, cursoNombre = "Matemáticas" },
                IdUsuario = 1
            };

            // Act
            var result = await _controller.CrearNotificacion(createDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var notificacion = createdResult.Value.Should().BeOfType<NotificacionDto>().Subject;
            notificacion.Metadata.Should().NotBeNull();
        }
    }
}
