using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Services;
using API_REST_CURSOSACADEMICOS.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    /// <summary>
    /// Controlador de autenticación que expone endpoints REST
    /// Sigue el patrón MVC y principios RESTful
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserLookupService _userLookupService;

        public AuthController(
            IAuthService authService, 
            ILogger<AuthController> logger,
            IConfiguration configuration,
            IUserLookupService userLookupService)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
            _userLookupService = userLookupService;
        }

        /// <summary>
        /// Endpoint de login - POST /api/auth/login
        /// </summary>
        /// <param name="loginDto">Credenciales del usuario</param>
        /// <returns>Tokens JWT y información del usuario</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Intento de login para: {loginDto?.Email ?? "null"}");

                if (loginDto == null)
                {
                    _logger.LogWarning("LoginDto es null");
                    return BadRequest(new { message = "Datos de entrada inválidos" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                    _logger.LogWarning($"ModelState inválido: {string.Join(", ", errors)}");
                    return BadRequest(new { 
                        message = "Datos de entrada inválidos", 
                        errors = errors
                    });
                }

                _logger.LogInformation($"Validando credenciales para: {loginDto.Email}");
                var result = await _authService.LoginAsync(loginDto);

                if (result == null)
                {
                    _logger.LogWarning($"Login fallido para: {loginDto.Email}");
                    return Unauthorized(new { message = "Email o contraseña incorrectos" });
                }

                _logger.LogInformation($"Login exitoso para: {loginDto.Email}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en Login para: {loginDto?.Email ?? "null"}");
                return StatusCode(500, new { 
                    message = "Error interno del servidor",
                    detail = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Endpoint para refrescar el token - POST /api/auth/refresh
        /// </summary>
        /// <param name="refreshTokenDto">Token actual y refresh token</param>
        /// <returns>Nuevos tokens JWT</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Datos de entrada inválidos" });
                }

                var result = await _authService.RefreshTokenAsync(refreshTokenDto);

                if (result == null)
                {
                    return Unauthorized(new { message = "Token inválido o expirado" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RefreshToken");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Endpoint de logout - POST /api/auth/logout
        /// </summary>
        /// <param name="email">Email del usuario</param>
        /// <returns>Confirmación de logout</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email requerido" });
                }

                var result = await _authService.RevokeTokenAsync(email);

                if (!result)
                {
                    return BadRequest(new { message = "No se pudo cerrar sesión" });
                }

                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Logout");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Endpoint para validar token - GET /api/auth/validate
        /// </summary>
        /// <returns>Validación del token actual</returns>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token no proporcionado" });
                }

                var isValid = await _authService.ValidateTokenAsync(token);

                if (!isValid)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                return Ok(new { message = "Token válido", isAuthenticated = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ValidateToken");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Endpoint para obtener información del usuario autenticado - GET /api/auth/me
        /// </summary>
        /// <returns>Información del usuario actual</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UsuarioDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var email = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                var usuario = await _authService.GetUsuarioByEmailAsync(email);

                if (usuario == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var usuarioDto = new UsuarioDto
                {
                    Id = usuario.Id,
                    Email = usuario.Email,
                    Nombres = usuario.Nombres,
                    Apellidos = usuario.Apellidos,
                    NombreCompleto = usuario.NombreCompleto,
                    Rol = usuario.Rol,
                    Estado = usuario.Estado,
                    FechaCreacion = usuario.FechaCreacion,
                    UltimoAcceso = usuario.UltimoAcceso
                };

                return Ok(usuarioDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetCurrentUser");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Endpoint para cambiar contraseña - POST /api/auth/change-password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { 
                        message = "Datos de entrada inválidos", 
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Obtener email del token JWT
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                var result = await _authService.ChangePasswordAsync(email, changePasswordDto);

                if (!result)
                {
                    return BadRequest(new { message = "No se pudo cambiar la contraseña. Verifica que la contraseña actual sea correcta" });
                }

                return Ok(new { message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ChangePassword");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Endpoint de login para docentes - POST /api/auth/docente/login
        /// </summary>
        /// <param name="loginDto">Correo y contraseña del docente</param>
        /// <returns>Token JWT con rol Docente</returns>
        [HttpPost("docente/login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthDocenteResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginDocente([FromBody] LoginDocenteDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Iniciando login para docente: {loginDto.Correo}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState inválido");
                    return BadRequest(new { 
                        message = "Datos de entrada inválidos", 
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                // Buscar docente por correo
                _logger.LogInformation($"Buscando docente con correo: {loginDto.Correo}");
                var docente = await _userLookupService.GetDocenteByEmailAsync(loginDto.Correo);

                if (docente == null)
                {
                    _logger.LogWarning($"Intento de login fallido: Docente no encontrado con correo {loginDto.Correo}");
                    return Unauthorized(new { message = "Correo o contraseña incorrectos" });
                }

                _logger.LogInformation($"Docente encontrado: {docente.Id} - {docente.Nombres}");

                // Verificar si tiene contraseña configurada
                if (string.IsNullOrEmpty(docente.PasswordHash))
                {
                    _logger.LogWarning($"Intento de login fallido: Docente {loginDto.Correo} no tiene contraseña configurada");
                    return Unauthorized(new { message = "No tiene contraseña configurada. Contacte al administrador." });
                }

                _logger.LogInformation("Verificando contraseña con BCrypt...");
                // Verificar contraseña con BCrypt
                bool passwordValida = false;
                try
                {
                    passwordValida = BCrypt.Net.BCrypt.Verify(loginDto.Password, docente.PasswordHash);
                    _logger.LogInformation($"Resultado verificación BCrypt: {passwordValida}");
                }
                catch (Exception bcryptEx)
                {
                    _logger.LogError(bcryptEx, "Error al verificar contraseña con BCrypt");
                    return StatusCode(500, new { message = "Error al verificar contraseña", detail = bcryptEx.Message });
                }

                if (!passwordValida)
                {
                    _logger.LogWarning($"Intento de login fallido: Contraseña incorrecta para {loginDto.Correo}");
                    return Unauthorized(new { message = "Correo o contraseña incorrectos" });
                }

                _logger.LogInformation("Generando token JWT...");
                // Generar tokens JWT
                string token;
                try
                {
                    token = GenerarTokenDocente(docente);
                    _logger.LogInformation("Token JWT generado exitosamente");
                }
                catch (Exception jwtEx)
                {
                    _logger.LogError(jwtEx, "Error al generar token JWT");
                    return StatusCode(500, new { message = "Error al generar token", detail = jwtEx.Message });
                }

                var refreshToken = GenerarRefreshToken();

                _logger.LogInformation($"Login exitoso para docente: {docente.Correo}");

                // Obtener minutos de expiración desde configuración (default: 30 minutos)
                var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out int mins) ? mins : 30;

                return Ok(new AuthDocenteResponseDto
                {
                    Id = docente.Id,
                    NombreCompleto = $"{docente.Nombres} {docente.Apellidos}",
                    Correo = docente.Correo!,
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiracion = DateTime.UtcNow.AddMinutes(expirationMinutes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en LoginDocente");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Genera un token JWT para el docente
        /// </summary>
        private string GenerarTokenDocente(Docente docente)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, docente.Id.ToString()),
                new Claim(ClaimTypes.Email, docente.Correo ?? ""),
                new Claim(ClaimTypes.Name, $"{docente.Nombres} {docente.Apellidos}"),
                new Claim(ClaimTypes.Role, "Docente"),
                new Claim("DocenteId", docente.Id.ToString())
            };

            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "";
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey no está configurado en appsettings.json");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Obtener minutos de expiración desde configuración (default: 30 minutos)
            var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out int minutes) ? minutes : 30;

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Genera un refresh token aleatorio
        /// </summary>
        private string GenerarRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Endpoint de prueba para generar hash BCrypt - GET /api/auth/test-bcrypt
        /// Solo para administradores
        /// </summary>
        [HttpGet("test-bcrypt")]
        [Authorize(Roles = "Administrador")]
        public IActionResult TestBCrypt([FromQuery] string password = "Admin123!")
        {
            try
            {
                // Generar hash
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                
                // Verificar que funciona
                bool verifica = BCrypt.Net.BCrypt.Verify(password, hash);
                
                return Ok(new
                {
                    password = password,
                    hash = hash,
                    hashLength = hash.Length,
                    verificaCorrectamente = verifica,
                    sql = new[]
                    {
                        $"UPDATE Usuario SET password_hash = '{hash}' WHERE email = 'admin@gestionacademica.com';",
                        $"UPDATE Usuario SET password_hash = '{hash}' WHERE email = 'docente@gestionacademica.com';",
                        $"UPDATE Usuario SET password_hash = '{hash}' WHERE email = 'coordinador@gestionacademica.com';"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TestBCrypt");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        #region Recuperación de Contraseña

        /// <summary>
        /// Solicita recuperación de contraseña - POST /api/auth/forgot-password
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ForgotPasswordResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, [FromServices] IPasswordResetService passwordResetService)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "El formato del correo electrónico no es válido."
                    });
                }

                var result = await passwordResetService.RequestPasswordResetAsync(request.Email, request.TipoUsuario);

                // Siempre devolver 200 para no revelar si el email existe
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ForgotPassword");
                return Ok(new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Error al procesar la solicitud. Por favor, intenta nuevamente."
                });
            }
        }

        /// <summary>
        /// Valida token de recuperación - POST /api/auth/validate-reset-token
        /// </summary>
        [HttpPost("validate-reset-token")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValidateTokenResponseDto))]
        public async Task<IActionResult> ValidateResetToken([FromBody] ValidateTokenDto request, [FromServices] IPasswordResetService passwordResetService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return Ok(new ValidateTokenResponseDto
                    {
                        Valid = false,
                        Message = "Token no proporcionado."
                    });
                }

                var result = await passwordResetService.ValidateTokenAsync(request.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ValidateResetToken");
                return Ok(new ValidateTokenResponseDto
                {
                    Valid = false,
                    Message = "Error al validar el token."
                });
            }
        }

        /// <summary>
        /// Resetea la contraseña - POST /api/auth/reset-password
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResetPasswordResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request, [FromServices] IPasswordResetService passwordResetService)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new ResetPasswordResponseDto
                    {
                        Success = false,
                        Message = string.Join(" ", errors)
                    });
                }

                var result = await passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ResetPassword");
                return Ok(new ResetPasswordResponseDto
                {
                    Success = false,
                    Message = "Error al actualizar la contraseña. Por favor, intenta nuevamente."
                });
            }
        }

        #endregion
    }
}
