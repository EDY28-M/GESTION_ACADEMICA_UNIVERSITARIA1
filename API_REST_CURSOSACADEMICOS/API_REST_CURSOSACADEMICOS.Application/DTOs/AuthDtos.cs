using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs DE AUTENTICACIÓN GENERAL
    // ============================================

    /// <summary>
    /// DTO para login general de usuarios
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de usuario esperado: "Administrador" o "Estudiante". 
        /// Si se proporciona, el backend validará que el usuario tenga ese rol específico.
        /// </summary>
        public string? TipoUsuario { get; set; }
    }

    /// <summary>
    /// DTO de respuesta de autenticación general
    /// </summary>
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UsuarioDto Usuario { get; set; } = new UsuarioDto();
    }

    /// <summary>
    /// DTO para refresh token
    /// </summary>
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "El refresh token es requerido")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para solicitud de cambio de contraseña
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ============================================
    // DTOs DE USUARIO
    // ============================================

    /// <summary>
    /// DTO para información de usuario
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
