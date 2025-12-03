using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Services
{
    /// <summary>
    /// Servicio de autenticación que implementa la lógica de negocio
    /// Utiliza BCrypt para hash de contraseñas y JWT para tokens
    /// Sigue el patrón Repository implícito a través de EF Core
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly GestionAcademicaContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            GestionAcademicaContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Buscar usuario por email
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

                if (usuario == null || !usuario.Estado)
                {
                    _logger.LogWarning($"Intento de login fallido para: {loginDto.Email}");
                    return null;
                }

                // Verificar contraseña con BCrypt
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
                {
                    _logger.LogWarning($"Contraseña incorrecta para: {loginDto.Email}");
                    return null;
                }

                // Generar tokens
                var token = GenerateJwtToken(usuario);
                var refreshToken = GenerateRefreshToken();

                // Actualizar refresh token y último acceso
                usuario.RefreshToken = refreshToken;
                usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                usuario.UltimoAcceso = DateTime.UtcNow;
                usuario.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Login exitoso para: {loginDto.Email}");

                return new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                    Usuario = MapToUsuarioDto(usuario)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en LoginAsync para: {loginDto.Email}");
                throw;
            }
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(refreshTokenDto.Token);
                if (principal == null)
                {
                    _logger.LogWarning("Token inválido en RefreshTokenAsync");
                    return null;
                }

                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }

                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (usuario == null || 
                    usuario.RefreshToken != refreshTokenDto.RefreshToken || 
                    usuario.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    _logger.LogWarning($"Refresh token inválido o expirado para: {email}");
                    return null;
                }

                // Generar nuevos tokens
                var newToken = GenerateJwtToken(usuario);
                var newRefreshToken = GenerateRefreshToken();

                usuario.RefreshToken = newRefreshToken;
                usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                usuario.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Token refrescado exitosamente para: {email}");

                return new AuthResponseDto
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                    Usuario = MapToUsuarioDto(usuario)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RefreshTokenAsync");
                throw;
            }
        }

        public async Task<bool> RevokeTokenAsync(string email)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (usuario == null)
                {
                    return false;
                }

                usuario.RefreshToken = null;
                usuario.RefreshTokenExpiry = null;
                usuario.FechaActualizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Token revocado para: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en RevokeTokenAsync para: {email}");
                throw;
            }
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(false);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetJwtSecretKey());

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<Usuario?> GetUsuarioByEmailAsync(string email)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto)
        {
            var usuario = await GetUsuarioByEmailAsync(email);
            
            if (usuario == null || !usuario.Estado)
            {
                _logger.LogWarning($"Usuario no encontrado o inactivo: {email}");
                return false;
            }

            // Verificar contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, usuario.PasswordHash))
            {
                _logger.LogWarning($"Contraseña actual incorrecta para: {email}");
                return false;
            }

            // Hashear nueva contraseña
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            usuario.FechaActualizacion = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Contraseña actualizada para: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cambiar contraseña para: {email}");
                return false;
            }
        }

        #region Métodos Privados

        private string GenerateJwtToken(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey()));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.GivenName, usuario.Nombres),
                new Claim(ClaimTypes.Surname, usuario.Apellidos),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("estado", usuario.Estado.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecretKey())),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = false // No validar expiración para poder refrescar
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private UsuarioDto MapToUsuarioDto(Usuario usuario)
        {
            return new UsuarioDto
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
        }

        private string GetJwtSecretKey()
        {
            return _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey no configurada");
        }

        private int GetTokenExpirationMinutes()
        {
            return int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out int minutes) 
                ? minutes 
                : 30;
        }

        #endregion
    }
}
