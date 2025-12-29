using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Auth;

public class AuthControllerBcryptTests
{
    [Fact]
    public void TestBCrypt_WithCustomPassword_ReturnsOkAndVerifiesCorrectly()
    {
        // Arrange
        var authService = new Mock<IAuthService>();
        var logger = new Mock<ILogger<AuthController>>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var userLookupService = new Mock<IUserLookupService>();

        var controller = new AuthController(
            authService.Object,
            logger.Object,
            configuration,
            userLookupService.Object);

        // Act
        var result = controller.TestBCrypt(password: "MiPasswordSegura123!");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();

        var value = ok.Value!;
        var hash = value.GetType().GetProperty("hash")?.GetValue(value) as string;
        var hashLength = value.GetType().GetProperty("hashLength")?.GetValue(value) as int?;
        var verifica = value.GetType().GetProperty("verificaCorrectamente")?.GetValue(value) as bool?;

        hash.Should().NotBeNullOrWhiteSpace();
        hashLength.Should().BeGreaterThan(0);
        verifica.Should().BeTrue();
    }
}

