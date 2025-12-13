using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces
{
    /// <summary>
    /// Interfaz del servicio de autenticación siguiendo el principio de Inversión de Dependencias (SOLID)
    /// Permite desacoplar la implementación de la lógica de negocio
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario y genera tokens JWT
        /// </summary>
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Refresca el token de acceso usando el refresh token
        /// </summary>
        Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

        /// <summary>
        /// Revoca el refresh token del usuario (logout)
        /// </summary>
        Task<bool> RevokeTokenAsync(string email);

        /// <summary>
        /// Verifica si un token JWT es válido
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Obtiene un usuario por su email
        /// </summary>
        Task<Usuario?> GetUsuarioByEmailAsync(string email);

        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        Task<bool> ChangePasswordAsync(string email, ChangePasswordDto changePasswordDto);
    }
}
