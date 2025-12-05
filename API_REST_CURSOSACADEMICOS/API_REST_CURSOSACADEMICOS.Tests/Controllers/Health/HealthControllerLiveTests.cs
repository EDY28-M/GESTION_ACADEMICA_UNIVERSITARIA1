using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Data;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Health;

/// <summary>
/// Tests adicionales para HealthController.
/// Complementa los tests existentes con cobertura del endpoint Live.
/// </summary>
public class HealthControllerLiveTests : IDisposable
{
    private readonly GestionAcademicaContext _context;
    private readonly HealthController _controller;
    private readonly Mock<ILogger<HealthController>> _loggerMock;

    public HealthControllerLiveTests()
    {
        var options = new DbContextOptionsBuilder<GestionAcademicaContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_HealthLive_{Guid.NewGuid()}")
            .Options;

        _context = new GestionAcademicaContext(options);
        _loggerMock = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_context, _loggerMock.Object);
    }

    #region Tests de Live endpoint

    [Fact]
    public void Live_ReturnsOkResult()
    {
        // Act
        var result = _controller.Live();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Live_ReturnsAliveStatus()
    {
        // Act
        var result = _controller.Live() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var status = response!.GetType().GetProperty("status")?.GetValue(response);
        status.Should().Be("Alive");
    }

    [Fact]
    public void Live_ReturnsTimestamp()
    {
        // Act
        var beforeCall = DateTime.UtcNow;
        var result = _controller.Live() as OkObjectResult;
        var afterCall = DateTime.UtcNow;
        var response = result!.Value;

        // Assert
        var timestamp = (DateTime?)response!.GetType().GetProperty("timestamp")?.GetValue(response);
        timestamp.Should().NotBeNull();
        timestamp.Should().BeOnOrAfter(beforeCall);
        timestamp.Should().BeOnOrBefore(afterCall);
    }

    [Fact]
    public void Live_ReturnsProcessId()
    {
        // Act
        var result = _controller.Live() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var processId = response!.GetType().GetProperty("processId")?.GetValue(response);
        processId.Should().NotBeNull();
        ((int)processId!).Should().BePositive();
    }

    [Fact]
    public void Live_ReturnsInstanceInfo()
    {
        // Act
        var result = _controller.Live() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var instance = response!.GetType().GetProperty("instance")?.GetValue(response);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void Live_AlwaysReturnsOk_RegardlessOfDbState()
    {
        // Arrange - No necesita setup especial, 
        // el endpoint Live no verifica la base de datos

        // Act
        var result = _controller.Live();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Live_IsIdempotent()
    {
        // Act - Llamar múltiples veces
        var result1 = _controller.Live() as OkObjectResult;
        var result2 = _controller.Live() as OkObjectResult;
        var result3 = _controller.Live() as OkObjectResult;

        // Assert - Todas deberían retornar Ok con status "Alive"
        var status1 = result1!.Value!.GetType().GetProperty("status")?.GetValue(result1.Value);
        var status2 = result2!.Value!.GetType().GetProperty("status")?.GetValue(result2.Value);
        var status3 = result3!.Value!.GetType().GetProperty("status")?.GetValue(result3.Value);

        status1.Should().Be("Alive");
        status2.Should().Be("Alive");
        status3.Should().Be("Alive");
    }

    #endregion

    #region Tests de Get (basic health)

    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Act
        var result = _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Get_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var status = response!.GetType().GetProperty("status")?.GetValue(response);
        status.Should().Be("Healthy");
    }

    [Fact]
    public void Get_ReturnsVersionInfo()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var version = response!.GetType().GetProperty("version")?.GetValue(response);
        version.Should().NotBeNull();
        version.Should().Be("1.0.0");
    }

    [Fact]
    public void Get_ReturnsMachineName()
    {
        // Act
        var result = _controller.Get() as OkObjectResult;
        var response = result!.Value;

        // Assert
        var machineName = response!.GetType().GetProperty("machineName")?.GetValue(response);
        machineName.Should().NotBeNull();
        machineName.Should().Be(Environment.MachineName);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
