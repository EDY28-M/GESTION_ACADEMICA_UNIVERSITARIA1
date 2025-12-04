using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Health
{
    public class HealthControllerTests
    {
        private readonly GestionAcademicaContext _context;
        private readonly Mock<ILogger<HealthController>> _mockLogger;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new GestionAcademicaContext(options);
            _mockLogger = new Mock<ILogger<HealthController>>();
            _controller = new HealthController(_context, _mockLogger.Object);
        }

        [Fact]
        public void Get_ReturnsOkWithHealthStatus()
        {
            // Act
            var result = _controller.Get();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDetailed_ReturnsObjectResult()
        {
            // Act
            var result = await _controller.GetDetailed();

            // Assert
            // GetDetailed puede devolver Ok (200) u ObjectResult con status 503, ambos son válidos
            result.Should().BeAssignableTo<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Ready_WithInMemoryDatabase_ReturnsOk()
        {
            // Act
            var result = await _controller.Ready();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void Live_ReturnsOkWithAliveStatus()
        {
            // Act
            var result = _controller.Live();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void Get_ContainsRequiredFields()
        {
            // Act
            var result = _controller.Get() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;
            value.Should().NotBeNull();
            
            // Verificar que el resultado contiene los campos esperados
            var properties = value!.GetType().GetProperties();
            var propertyNames = properties.Select(p => p.Name.ToLower()).ToList();
            
            propertyNames.Should().Contain("status");
            propertyNames.Should().Contain("timestamp");
        }

        [Fact]
        public void Live_ContainsStatusAlive()
        {
            // Act
            var result = _controller.Live() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;
            value.Should().NotBeNull();

            var properties = value!.GetType().GetProperties();
            var statusProp = properties.FirstOrDefault(p => p.Name.Equals("status", StringComparison.OrdinalIgnoreCase));
            statusProp.Should().NotBeNull();
            
            var statusValue = statusProp!.GetValue(value);
            statusValue.Should().Be("Alive");
        }

        [Fact]
        public async Task Ready_ContainsStatusField()
        {
            // Act
            var result = await _controller.Ready() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;
            value.Should().NotBeNull();

            var properties = value!.GetType().GetProperties();
            var statusProp = properties.FirstOrDefault(p => p.Name.Equals("status", StringComparison.OrdinalIgnoreCase));
            statusProp.Should().NotBeNull();
            
            var statusValue = statusProp!.GetValue(value)?.ToString();
            statusValue.Should().Be("Ready");
        }
    }
}
