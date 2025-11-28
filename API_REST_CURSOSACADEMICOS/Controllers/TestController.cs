using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("hash/{password}")]
        public IActionResult GenerateHash(string password)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(new { password, hash });
        }

        [HttpPost("verify")]
        public IActionResult VerifyHash([FromBody] VerifyRequest request)
        {
            var isValid = BCrypt.Net.BCrypt.Verify(request.Password, request.Hash);
            return Ok(new { isValid, password = request.Password, hash = request.Hash });
        }
    }

    public class VerifyRequest
    {
        public string Password { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }
}
