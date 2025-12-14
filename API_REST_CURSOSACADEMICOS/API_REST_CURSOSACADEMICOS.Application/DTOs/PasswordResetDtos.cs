using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    /// <summary>
    /// DTO para solicitar recuperación de contraseña
    /// </summary>
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de usuario: "Usuario" (Admin), "Docente", o "Estudiante"
        /// Requerido cuando el correo existe en múltiples tipos de cuenta
        /// </summary>
        public string? TipoUsuario { get; set; }
    }

    /// <summary>
    /// DTO para resetear la contraseña
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para validar un token de recuperación
    /// </summary>
    public class ValidateTokenDto
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de la solicitud de recuperación de contraseña
    /// </summary>
    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        /// <summary>
        /// Token para desarrollo (quitar en producción)
        /// </summary>
        public string? Token { get; set; }
    }

    /// <summary>
    /// Respuesta del reset de contraseña
    /// </summary>
    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de validación de token
    /// </summary>
    public class ValidateTokenResponseDto
    {
        public bool Valid { get; set; }
        public string? Email { get; set; }
        public string? TipoUsuario { get; set; }
        public string? Message { get; set; }
    }
}
