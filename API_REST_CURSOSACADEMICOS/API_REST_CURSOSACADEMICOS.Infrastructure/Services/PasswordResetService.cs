using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace API_REST_CURSOSACADEMICOS.Services
{
    public interface IPasswordResetService
    {
        Task<ForgotPasswordResponseDto> RequestPasswordResetAsync(string email, string? tipoUsuario = null);
        Task<ValidateTokenResponseDto> ValidateTokenAsync(string token);
        Task<ResetPasswordResponseDto> ResetPasswordAsync(string token, string newPassword);
    }

    public class PasswordResetService : IPasswordResetService
    {
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public PasswordResetService(
            GestionAcademicaContext context,
            ILogger<PasswordResetService> logger,
            IConfiguration configuration,
            EmailService emailService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
        }

        /// <summary>
        /// Solicita un token de recuperación de contraseña
        /// </summary>
        public async Task<ForgotPasswordResponseDto> RequestPasswordResetAsync(string email, string? tipoUsuarioSolicitado = null)
        {
            try
            {
                email = email.Trim().ToLower();

                // Buscar en todas las tablas que pueden tener usuarios
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email);
                var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Correo != null && d.Correo.ToLower() == email);
                var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.Correo != null && e.Correo.ToLower() == email);

                // Contar cuántas coincidencias hay
                int coincidencias = 0;
                if (usuario != null) coincidencias++;
                if (docente != null) coincidencias++;
                if (estudiante != null) coincidencias++;

                string tipoUsuario = "";
                int? idUsuario = null;
                int? idDocente = null;
                int? idEstudiante = null;

                // Si hay múltiples coincidencias, requerir que se especifique el tipo
                if (coincidencias > 1)
                {
                    if (string.IsNullOrWhiteSpace(tipoUsuarioSolicitado))
                    {
                        var tiposEncontrados = new List<string>();
                        if (usuario != null) tiposEncontrados.Add("Administrador");
                        if (docente != null) tiposEncontrados.Add("Docente");
                        if (estudiante != null) tiposEncontrados.Add("Estudiante");

                        return new ForgotPasswordResponseDto
                        {
                            Success = false,
                            Message = $"Este correo electrónico está asociado a múltiples cuentas ({string.Join(", ", tiposEncontrados)}). Por favor, especifica el tipo de cuenta desde el portal correspondiente."
                        };
                    }

                    // Validar el tipo solicitado
                    tipoUsuarioSolicitado = tipoUsuarioSolicitado.Trim();
                    if (tipoUsuarioSolicitado.Equals("Usuario", StringComparison.OrdinalIgnoreCase) || 
                        tipoUsuarioSolicitado.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || 
                        tipoUsuarioSolicitado.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        if (usuario == null)
                        {
                            return new ForgotPasswordResponseDto
                            {
                                Success = false,
                                Message = "No se encontró una cuenta de Administrador con este correo electrónico."
                            };
                        }
                        tipoUsuario = "Usuario";
                        idUsuario = usuario.Id;
                    }
                    else if (tipoUsuarioSolicitado.Equals("Docente", StringComparison.OrdinalIgnoreCase))
                    {
                        if (docente == null)
                        {
                            return new ForgotPasswordResponseDto
                            {
                                Success = false,
                                Message = "No se encontró una cuenta de Docente con este correo electrónico."
                            };
                        }
                        tipoUsuario = "Docente";
                        idDocente = docente.Id;
                    }
                    else if (tipoUsuarioSolicitado.Equals("Estudiante", StringComparison.OrdinalIgnoreCase))
                    {
                        if (estudiante == null)
                        {
                            return new ForgotPasswordResponseDto
                            {
                                Success = false,
                                Message = "No se encontró una cuenta de Estudiante con este correo electrónico."
                            };
                        }
                        tipoUsuario = "Estudiante";
                        idEstudiante = estudiante.Id;
                    }
                    else
                    {
                        return new ForgotPasswordResponseDto
                        {
                            Success = false,
                            Message = "Tipo de usuario no válido. Debe ser: Administrador, Docente o Estudiante."
                        };
                    }
                }
                else if (coincidencias == 1)
                {
                    // Solo una coincidencia, pero si se especificó un tipo, validarlo
                    if (!string.IsNullOrWhiteSpace(tipoUsuarioSolicitado))
                    {
                        tipoUsuarioSolicitado = tipoUsuarioSolicitado.Trim();
                        // Validar que el tipo solicitado coincida con el encontrado
                        if ((tipoUsuarioSolicitado.Equals("Usuario", StringComparison.OrdinalIgnoreCase) || 
                             tipoUsuarioSolicitado.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || 
                             tipoUsuarioSolicitado.Equals("Admin", StringComparison.OrdinalIgnoreCase)) && usuario != null)
                        {
                            tipoUsuario = "Usuario";
                            idUsuario = usuario.Id;
                        }
                        else if (tipoUsuarioSolicitado.Equals("Docente", StringComparison.OrdinalIgnoreCase) && docente != null)
                        {
                            tipoUsuario = "Docente";
                            idDocente = docente.Id;
                        }
                        else if (tipoUsuarioSolicitado.Equals("Estudiante", StringComparison.OrdinalIgnoreCase) && estudiante != null)
                        {
                            tipoUsuario = "Estudiante";
                            idEstudiante = estudiante.Id;
                        }
                        else
                        {
                            // El tipo solicitado no coincide con el encontrado
                            return new ForgotPasswordResponseDto
                            {
                                Success = false,
                                Message = $"El tipo de cuenta especificado no coincide con el correo electrónico. Por favor, usa el portal correcto."
                            };
                        }
                    }
                    else
                    {
                        // No se especificó tipo, usar el encontrado
                        if (usuario != null)
                        {
                            tipoUsuario = "Usuario";
                            idUsuario = usuario.Id;
                        }
                        else if (docente != null)
                        {
                            tipoUsuario = "Docente";
                            idDocente = docente.Id;
                        }
                        else if (estudiante != null)
                        {
                            tipoUsuario = "Estudiante";
                            idEstudiante = estudiante.Id;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Intento de recuperación de contraseña para email no registrado: {Email}", email);
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Usuario no registrado. El correo electrónico ingresado no está asociado a ninguna cuenta."
                    };
                }

                // Invalidar tokens anteriores para este email
                var tokensAnteriores = await _context.PasswordResetTokens
                    .Where(t => t.Email.ToLower() == email && !t.Usado)
                    .ToListAsync();

                foreach (var token in tokensAnteriores)
                {
                    token.Usado = true;
                }

                // Generar nuevo token
                var nuevoToken = GenerateSecureToken();
                var expiracion = DateTime.Now.AddHours(24); // Token válido por 24 horas

                var resetToken = new PasswordResetToken
                {
                    Email = email,
                    Token = nuevoToken,
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = expiracion,
                    Usado = false,
                    TipoUsuario = tipoUsuario,
                    IdUsuario = idUsuario,
                    IdDocente = idDocente,
                    IdEstudiante = idEstudiante
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                // Obtener nombre del usuario para el email
                string userName = "";
                if (usuario != null) userName = $"{usuario.Nombres} {usuario.Apellidos}";
                else if (docente != null) userName = $"{docente.Nombres} {docente.Apellidos}";
                else if (estudiante != null) userName = $"{estudiante.Nombres} {estudiante.Apellidos}";

                // Enviar email de recuperación
                var emailEnviado = await _emailService.SendPasswordResetEmailAsync(email, nuevoToken, userName, tipoUsuario);

                _logger.LogInformation(
                    "Token de recuperación generado para {Email} (expira: {Expiracion}) - Email enviado: {Enviado}",
                    email, expiracion, emailEnviado);

                var exposeToken = ShouldExposeResetToken();

                // Si el email no se pudo enviar pero estamos en modo desarrollo, aún devolver éxito
                // En producción, deberíamos informar al usuario
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

                if (!emailEnviado && !isDevelopment)
                {
                    _logger.LogWarning("⚠️ El token se generó pero el email no se pudo enviar para {Email}", email);
                    // Aún así devolvemos éxito para no revelar si el email existe
                    // Pero el usuario debería verificar su configuración de email
                }

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = emailEnviado 
                        ? "¡Instrucciones enviadas! Por favor, revisa tu bandeja de entrada (y la carpeta de spam)."
                        : "Token generado. Si no recibes el email, verifica la configuración del servidor SMTP.",
                    Email = MaskEmail(email),
                    Token = exposeToken ? nuevoToken : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar solicitud de recuperación de contraseña para {Email}", email);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Error al procesar la solicitud. Por favor, intenta nuevamente."
                };
            }
        }

        /// <summary>
        /// Valida si un token es válido
        /// </summary>
        public async Task<ValidateTokenResponseDto> ValidateTokenAsync(string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (resetToken == null)
                {
                    return new ValidateTokenResponseDto
                    {
                        Valid = false,
                        Message = "Token no válido o no encontrado."
                    };
                }

                if (resetToken.Usado)
                {
                    return new ValidateTokenResponseDto
                    {
                        Valid = false,
                        Message = "Este enlace ya fue utilizado. Solicita uno nuevo."
                    };
                }

                if (resetToken.FechaExpiracion < DateTime.Now)
                {
                    return new ValidateTokenResponseDto
                    {
                        Valid = false,
                        Message = "El enlace ha expirado. Solicita uno nuevo."
                    };
                }

                return new ValidateTokenResponseDto
                {
                    Valid = true,
                    Email = MaskEmail(resetToken.Email),
                    TipoUsuario = resetToken.TipoUsuario,
                    Message = "Token válido."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar token de recuperación");
                return new ValidateTokenResponseDto
                {
                    Valid = false,
                    Message = "Error al validar el token."
                };
            }
        }

        /// <summary>
        /// Resetea la contraseña usando el token
        /// </summary>
        public async Task<ResetPasswordResponseDto> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (resetToken == null)
                {
                    return new ResetPasswordResponseDto
                    {
                        Success = false,
                        Message = "Token no válido o no encontrado."
                    };
                }

                if (resetToken.Usado)
                {
                    return new ResetPasswordResponseDto
                    {
                        Success = false,
                        Message = "Este enlace ya fue utilizado."
                    };
                }

                if (resetToken.FechaExpiracion < DateTime.Now)
                {
                    return new ResetPasswordResponseDto
                    {
                        Success = false,
                        Message = "El enlace ha expirado."
                    };
                }

                // Obtener la contraseña actual para verificar que la nueva sea diferente
                string? currentPasswordHash = null;

                if (resetToken.TipoUsuario == "Usuario" && resetToken.IdUsuario.HasValue)
                {
                    var usuario = await _context.Usuarios.FindAsync(resetToken.IdUsuario.Value);
                    if (usuario != null)
                    {
                        currentPasswordHash = usuario.PasswordHash;
                    }
                }
                else if (resetToken.TipoUsuario == "Docente" && resetToken.IdDocente.HasValue)
                {
                    var docente = await _context.Docentes.FindAsync(resetToken.IdDocente.Value);
                    if (docente != null)
                    {
                        currentPasswordHash = docente.PasswordHash;
                    }
                }
                else if (resetToken.TipoUsuario == "Estudiante" && resetToken.IdEstudiante.HasValue)
                {
                    var estudiante = await _context.Estudiantes.FindAsync(resetToken.IdEstudiante.Value);
                    if (estudiante != null && estudiante.IdUsuario > 0)
                    {
                        var usuario = await _context.Usuarios.FindAsync(estudiante.IdUsuario);
                        if (usuario != null)
                        {
                            currentPasswordHash = usuario.PasswordHash;
                        }
                    }
                }

                // Verificar que la nueva contraseña sea diferente a la anterior
                if (!string.IsNullOrEmpty(currentPasswordHash))
                {
                    if (BCrypt.Net.BCrypt.Verify(newPassword, currentPasswordHash))
                    {
                        return new ResetPasswordResponseDto
                        {
                            Success = false,
                            Message = "La nueva contraseña debe ser diferente a la contraseña actual."
                        };
                    }
                }

                // Hash de la nueva contraseña
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Actualizar la contraseña según el tipo de usuario
                bool updated = false;

                if (resetToken.TipoUsuario == "Usuario" && resetToken.IdUsuario.HasValue)
                {
                    var usuario = await _context.Usuarios.FindAsync(resetToken.IdUsuario.Value);
                    if (usuario != null)
                    {
                        usuario.PasswordHash = passwordHash;
                        usuario.FechaActualizacion = DateTime.Now;
                        updated = true;
                    }
                }
                else if (resetToken.TipoUsuario == "Docente" && resetToken.IdDocente.HasValue)
                {
                    var docente = await _context.Docentes.FindAsync(resetToken.IdDocente.Value);
                    if (docente != null)
                    {
                        docente.PasswordHash = passwordHash;
                        updated = true;
                    }
                }
                else if (resetToken.TipoUsuario == "Estudiante" && resetToken.IdEstudiante.HasValue)
                {
                    // Si los estudiantes tienen su propio campo de contraseña
                    // Por ahora, actualizamos el usuario asociado si existe
                    var estudiante = await _context.Estudiantes.FindAsync(resetToken.IdEstudiante.Value);
                    if (estudiante != null && estudiante.IdUsuario > 0)
                    {
                        var usuario = await _context.Usuarios.FindAsync(estudiante.IdUsuario);
                        if (usuario != null)
                        {
                            usuario.PasswordHash = passwordHash;
                            usuario.FechaActualizacion = DateTime.Now;
                            updated = true;
                        }
                    }
                }

                if (!updated)
                {
                    return new ResetPasswordResponseDto
                    {
                        Success = false,
                        Message = "No se pudo actualizar la contraseña. Contacta a soporte."
                    };
                }

                // Marcar token como usado
                resetToken.Usado = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Contraseña actualizada exitosamente para {Email} ({TipoUsuario})",
                    resetToken.Email, resetToken.TipoUsuario);

                return new ResetPasswordResponseDto
                {
                    Success = true,
                    Message = "¡Contraseña actualizada exitosamente! Ya puedes iniciar sesión."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña");
                return new ResetPasswordResponseDto
                {
                    Success = false,
                    Message = "Error al actualizar la contraseña. Por favor, intenta nuevamente."
                };
            }
        }

        /// <summary>
        /// Genera un token seguro aleatorio
        /// </summary>
        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        /// <summary>
        /// Enmascara el email para mostrar parcialmente
        /// </summary>
        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 2)
                return $"{name[0]}***@{domain}";

            return $"{name[0]}{name[1]}***@{domain}";
        }

        /// <summary>
        /// Simula el envío de email (para desarrollo)
        /// En producción, integrar con un servicio real como SendGrid, AWS SES, etc.
        /// </summary>
        private async Task SimularEnvioEmail(string email, string token)
        {
            // Construir URL de reset (ajustar según tu frontend)
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
            var resetUrl = $"{frontendUrl}/reset-password?token={token}";

            _logger.LogInformation(
                "========== EMAIL DE RECUPERACIÓN (SIMULADO) ==========\n" +
                "Para: {Email}\n" +
                "Asunto: Recuperación de Contraseña - Academia Universitaria\n" +
                "Enlace de recuperación: {ResetUrl}\n" +
                "Expira en: 24 horas\n" +
                "======================================================",
                email, resetUrl);

            await Task.CompletedTask;
        }

        private bool ShouldExposeResetToken()
        {
            var configured = _configuration["AppSettings:ExposePasswordResetToken"];
            if (bool.TryParse(configured, out var value))
            {
                return value;
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}
