using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs BÁSICOS DE ESTUDIANTE
    // ============================================

    /// <summary>
    /// DTO para mostrar información básica de un estudiante
    /// </summary>
    public class EstudianteDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Información personal adicional
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Dni { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        
        // Información académica
        public int CicloActual { get; set; }
        public int CreditosAcumulados { get; set; }
        // Promedio acumulado de todos los semestres (histórico)
        public decimal? PromedioAcumulado { get; set; }
        // Promedio del semestre actual o último
        public decimal? PromedioSemestral { get; set; }
        public string Carrera { get; set; } = string.Empty;
        public DateTime FechaIngreso { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para el Orden de Mérito
    /// </summary>
    public class OrdenMeritoDto
    {
        public int Posicion { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto => $"{Nombres} {Apellidos}";
        public string Promocion { get; set; } = string.Empty;
        public int Semestre { get; set; }
        
        // CC - Créditos Llevados en el Semestre
        public int CreditosLlevadosSemestre { get; set; }
        
        // CA - Créditos Aprobados en el Semestre
        public int CreditosAprobadosSemestre { get; set; }
        
        // TCC - Total Créditos Llevados (acumulado)
        public int TotalCreditosLlevados { get; set; }
        
        // TCA - Total Créditos Aprobados (acumulado)
        public int TotalCreditosAprobados { get; set; }
        
        // PPS - Promedio Ponderado Semestral
        public decimal PromedioPonderadoSemestral { get; set; }
        
        // PPA - Promedio Ponderado Acumulado
        public decimal PromedioPonderadoAcumulado { get; set; }
        
        // Rango de mérito
        public string RangoMerito { get; set; } = string.Empty;
        
        public int TotalEstudiantes { get; set; }
        
        // Información del periodo de referencia
        public string? PeriodoNombre { get; set; }
        public string? EstadoPeriodo { get; set; }
    }

    /// <summary>
    /// DTO para crear un nuevo estudiante
    /// </summary>
    public class CrearEstudianteDto
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son requeridos")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es requerido")]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ciclo es requerido")]
        [Range(1, 10, ErrorMessage = "El ciclo debe estar entre 1 y 10")]
        public int Ciclo { get; set; }
    }

    /// <summary>
    /// DTO para el estado consolidado del estudiante desde la vista SQL
    /// </summary>
    public class EstudianteEstadoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int CicloActual { get; set; }
        public int CreditosAprobados { get; set; }
        public decimal? PromedioSemestral { get; set; }
        public decimal? PromedioAcumulado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? IdPeriodoActivo { get; set; }
        public string? PeriodoActivo { get; set; }
        public int CursosActivos { get; set; }
        public int? CursosAprobadosPeriodo { get; set; }
        public int? CursosDesaprobadosPeriodo { get; set; }
    }

    /// <summary>
    /// DTO para cambio de contraseña del estudiante
    /// </summary>
    public class CambiarContrasenaDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [JsonPropertyName("contrasenaActual")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [JsonPropertyName("contrasenaNueva")]
        public string ContrasenaNueva { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para actualizar información personal del estudiante
    /// </summary>
    public class ActualizarPerfilDto
    {
        [MaxLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        public string? Apellidos { get; set; }

        [MaxLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
        public string? Nombres { get; set; }

        [MaxLength(20, ErrorMessage = "El DNI no puede exceder 20 caracteres")]
        public string? Dni { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [MaxLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string? Correo { get; set; }

        [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Telefono { get; set; }

        [MaxLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string? Direccion { get; set; }
    }
}
