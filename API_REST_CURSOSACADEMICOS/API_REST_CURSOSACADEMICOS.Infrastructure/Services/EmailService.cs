using System;
using System.Net;
using System.Net.Mail;

namespace API_REST_CURSOSACADEMICOS.Services
{
    /// <summary>
    /// Servicio para envío de correos electrónicos mediante SMTP
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Envía un correo electrónico
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var password = _configuration["EmailSettings:Password"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                // Validar configuración básica
                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail))
                {
                    _logger.LogWarning("⚠️ Email no configurado correctamente. Configure EmailSettings en appsettings.json");
                    _logger.LogInformation($"📧 [SIMULADO] Email para: {toEmail}");
                    _logger.LogInformation($"📧 [SIMULADO] Asunto: {subject}");
                    return true; // Simular éxito en desarrollo
                }

                // Si el password no está en appsettings, permitir configurarlo por variables de entorno (recomendado)
                // - Docker Compose: EMAIL_PASSWORD -> EmailSettings__Password
                // - Local dev: setx EMAIL_PASSWORD "..."
                if (string.IsNullOrWhiteSpace(password) ||
                    password == "CHANGE_ME_APP_PASSWORD" ||
                    password == "TU_CONTRASEÑA_DE_APLICACION_AQUI")
                {
                    password =
                        Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ??
                        Environment.GetEnvironmentVariable("GMAIL_APP_PASSWORD") ??
                        password;
                }

                // Si sigue sin password, simular SOLO en Development (evitar falsas "ok" en producción)
                if (string.IsNullOrWhiteSpace(password) ||
                    password == "CHANGE_ME_APP_PASSWORD" ||
                    password == "TU_CONTRASEÑA_DE_APLICACION_AQUI")
                {
                    var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

                    if (environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("⚠️ Email sin password. Configura EMAIL_PASSWORD (recomendado) o EmailSettings:Password.");
                        _logger.LogInformation($"📧 [SIMULADO] Email para: {toEmail}");
                        _logger.LogInformation($"📧 [SIMULADO] Asunto: {subject}");
                        _logger.LogInformation($"📧 [SIMULADO] Para Gmail usa una 'Contraseña de aplicación' (2FA) y guárdala como secret/ENV.");
                        return true;
                    }

                    _logger.LogError("❌ Email no configurado: falta EmailSettings:Password / EMAIL_PASSWORD en entorno no-Development.");
                    return false;
                }

                // Configurar cliente SMTP con soporte para STARTTLS
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000,
                    UseDefaultCredentials = false
                };

                // Para Gmail, asegurar que se use STARTTLS correctamente
                if (smtpServer?.Contains("gmail.com") == true && smtpPort == 587)
                {
                    // Gmail requiere STARTTLS en el puerto 587
                    // EnableSsl = true ya maneja STARTTLS automáticamente
                    client.EnableSsl = true;
                }
                else if (smtpServer?.Contains("gmail.com") == true && smtpPort == 465)
                {
                    // Gmail puerto 465 usa SSL/TLS directo
                    client.EnableSsl = true;
                }

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                
                _logger.LogInformation($"✅ Email enviado exitosamente a: {toEmail}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError($"❌ Error SMTP al enviar email a {toEmail}: {smtpEx.Message}");
                _logger.LogError($"   StatusCode: {smtpEx.StatusCode}");
                
                // Mensajes más específicos según el error
                if (smtpEx.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
                {
                    _logger.LogError($"   💡 Solución: Asegúrate de que EnableSsl esté en 'true' y uses el puerto 587 para Gmail");
                }
                else if (smtpEx.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                         smtpEx.Message.Contains("5.7.0", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError($"   💡 Solución: Verifica que la contraseña de aplicación sea correcta");
                    _logger.LogError($"   💡 Para Gmail: Usa una 'Contraseña de aplicación', no tu contraseña normal");
                    _logger.LogError($"   💡 Pasos: Google Account -> Seguridad -> Verificación en 2 pasos -> Contraseñas de aplicación");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error al enviar email a {toEmail}: {ex.Message}");
                _logger.LogError($"   StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Envía el correo de recuperación de contraseña
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName, string tipoUsuario = "")
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
            
            // Construir la URL según el tipo de usuario
            string resetPath = tipoUsuario switch
            {
                "Docente" => "/docente/reset-password",
                "Estudiante" => "/estudiante/reset-password",
                "Usuario" => "/admin/reset-password",
                _ => "/reset-password" // Fallback
            };
            
            var resetLink = $"{frontendUrl}{resetPath}?token={Uri.EscapeDataString(resetToken)}";

            var subject = "🔐 Recuperación de Contraseña - Academia Global";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #003366 0%, #004488 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>
                ACADEMIA <span style='color: #C7A740;'>GLOBAL</span>
            </h1>
            <p style='color: rgba(255,255,255,0.8); margin: 10px 0 0 0; font-size: 14px;'>
                Sistema de Gestión Académica
            </p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px 30px;'>
            <h2 style='color: #003366; margin: 0 0 20px 0; font-size: 20px;'>
                Hola{(string.IsNullOrEmpty(userName) ? "" : $" {userName}")},
            </h2>
            
            <p style='color: #4a4a4a; line-height: 1.6; margin: 0 0 20px 0;'>
                Hemos recibido una solicitud para restablecer tu contraseña. Si no realizaste esta solicitud, puedes ignorar este correo.
            </p>
            
            <p style='color: #4a4a4a; line-height: 1.6; margin: 0 0 30px 0;'>
                Para restablecer tu contraseña, haz clic en el siguiente botón:
            </p>
            
            <!-- Button -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' 
                   style='display: inline-block; background: linear-gradient(135deg, #003366 0%, #004488 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 8px; font-weight: bold; font-size: 16px;'>
                    Restablecer Contraseña
                </a>
            </div>
            
            <p style='color: #888888; font-size: 12px; line-height: 1.6; margin: 30px 0 0 0;'>
                Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:
            </p>
            <p style='color: #003366; font-size: 12px; word-break: break-all; margin: 10px 0;'>
                {resetLink}
            </p>
            
            <!-- Warning -->
            <div style='background-color: #fff3cd; border-left: 4px solid #C7A740; padding: 15px; margin: 30px 0; border-radius: 0 8px 8px 0;'>
                <p style='color: #856404; margin: 0; font-size: 14px;'>
                    ⏰ <strong>Importante:</strong> Este enlace expirará en <strong>24 horas</strong> por seguridad.
                </p>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #e9ecef;'>
            <p style='color: #888888; font-size: 12px; margin: 0;'>
                Este es un correo automático, por favor no respondas a este mensaje.
            </p>
            <p style='color: #888888; font-size: 12px; margin: 10px 0 0 0;'>
                © 2025 Academia Global. Todos los derechos reservados.
            </p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body, true);
        }
    }
}
