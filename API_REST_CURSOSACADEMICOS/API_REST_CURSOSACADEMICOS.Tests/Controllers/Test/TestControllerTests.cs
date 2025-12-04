using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Test
{
    public class TestControllerTests
    {
        private readonly TestController _controller;

        public TestControllerTests()
        {
            _controller = new TestController();
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
        public void GenerateHash_WithValidPassword_ReturnsOkWithHash()
        {
            // Arrange
            SetupAdminUser();
            var password = "testPassword123";

            // Act
            var result = _controller.GenerateHash(password);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void GenerateHash_ReturnsPasswordAndHash()
        {
            // Arrange
            SetupAdminUser();
            var password = "mySecurePassword";

            // Act
            var result = _controller.GenerateHash(password) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;
            value.Should().NotBeNull();

            var properties = value!.GetType().GetProperties();
            var propertyNames = properties.Select(p => p.Name.ToLower()).ToList();
            
            propertyNames.Should().Contain("password");
            propertyNames.Should().Contain("hash");
        }

        [Fact]
        public void GenerateHash_HashIsDifferentFromPassword()
        {
            // Arrange
            SetupAdminUser();
            var password = "testPassword";

            // Act
            var result = _controller.GenerateHash(password) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;

            var properties = value!.GetType().GetProperties();
            var hashProp = properties.FirstOrDefault(p => p.Name.Equals("hash", StringComparison.OrdinalIgnoreCase));
            var passwordProp = properties.FirstOrDefault(p => p.Name.Equals("password", StringComparison.OrdinalIgnoreCase));

            var hashValue = hashProp!.GetValue(value)?.ToString();
            var passwordValue = passwordProp!.GetValue(value)?.ToString();

            hashValue.Should().NotBe(passwordValue);
            hashValue.Should().StartWith("$2"); // BCrypt hashes start with $2
        }

        [Fact]
        public void GenerateHash_WithEmptyPassword_StillGeneratesHash()
        {
            // Arrange
            SetupAdminUser();
            var password = "";

            // Act
            var result = _controller.GenerateHash(password);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void VerifyHash_WithValidPasswordAndHash_ReturnsValid()
        {
            // Arrange
            SetupAdminUser();
            var password = "testPassword123";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var request = new VerifyRequest
            {
                Password = password,
                Hash = hash
            };

            // Act
            var result = _controller.VerifyHash(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;
            value.Should().NotBeNull();

            var properties = value!.GetType().GetProperties();
            var isValidProp = properties.FirstOrDefault(p => p.Name.Equals("isValid", StringComparison.OrdinalIgnoreCase));
            
            isValidProp.Should().NotBeNull();
            var isValidValue = (bool)isValidProp!.GetValue(value)!;
            isValidValue.Should().BeTrue();
        }

        [Fact]
        public void VerifyHash_WithInvalidPassword_ReturnsInvalid()
        {
            // Arrange
            SetupAdminUser();
            var correctPassword = "correctPassword";
            var wrongPassword = "wrongPassword";
            var hash = BCrypt.Net.BCrypt.HashPassword(correctPassword);

            var request = new VerifyRequest
            {
                Password = wrongPassword,
                Hash = hash
            };

            // Act
            var result = _controller.VerifyHash(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;
            value.Should().NotBeNull();

            var properties = value!.GetType().GetProperties();
            var isValidProp = properties.FirstOrDefault(p => p.Name.Equals("isValid", StringComparison.OrdinalIgnoreCase));
            
            isValidProp.Should().NotBeNull();
            var isValidValue = (bool)isValidProp!.GetValue(value)!;
            isValidValue.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_ReturnsPasswordAndHashInResponse()
        {
            // Arrange
            SetupAdminUser();
            var password = "myPassword";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var request = new VerifyRequest
            {
                Password = password,
                Hash = hash
            };

            // Act
            var result = _controller.VerifyHash(request) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            var value = result!.Value;

            var properties = value!.GetType().GetProperties();
            var propertyNames = properties.Select(p => p.Name.ToLower()).ToList();
            
            propertyNames.Should().Contain("isvalid");
            propertyNames.Should().Contain("password");
            propertyNames.Should().Contain("hash");
        }

        [Fact]
        public void GenerateHash_MultipleCalls_GenerateDifferentHashes()
        {
            // Arrange
            SetupAdminUser();
            var password = "samePassword";

            // Act
            var result1 = _controller.GenerateHash(password) as OkObjectResult;
            var result2 = _controller.GenerateHash(password) as OkObjectResult;

            // Assert
            var value1 = result1!.Value;
            var value2 = result2!.Value;

            var properties1 = value1!.GetType().GetProperties();
            var properties2 = value2!.GetType().GetProperties();

            var hashProp1 = properties1.FirstOrDefault(p => p.Name.Equals("hash", StringComparison.OrdinalIgnoreCase));
            var hashProp2 = properties2.FirstOrDefault(p => p.Name.Equals("hash", StringComparison.OrdinalIgnoreCase));

            var hash1 = hashProp1!.GetValue(value1)?.ToString();
            var hash2 = hashProp2!.GetValue(value2)?.ToString();

            // BCrypt generates different hashes for the same password due to salt
            hash1.Should().NotBe(hash2);
        }
    }
}
