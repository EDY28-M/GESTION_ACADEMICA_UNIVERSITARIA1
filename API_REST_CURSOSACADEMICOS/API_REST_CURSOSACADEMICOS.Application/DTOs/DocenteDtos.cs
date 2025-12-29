using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs BÁSICOS DE DOCENTE
    // ============================================

    /// <summary>
    /// DTO principal para mostrar información de un docente
    /// </summary>
    public class DocenteDto
    {
        public int Id { get; set; }
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string? Profesion { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Correo { get; set; }
        public List<CursoSimpleDto> Cursos { get; set; } = new List<CursoSimpleDto>();
    }

    /// <summary>
    /// DTO para crear un docente básico (sin contraseña)
    /// </summary>
    public class DocenteCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Profesion { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Correo { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un docente básico (sin contraseña)
    /// </summary>
    public class DocenteUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Profesion { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Correo { get; set; }
    }

    /// <summary>
    /// DTO simplificado de curso
    /// </summary>
    public class CursoSimpleDto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public int Ciclo { get; set; }
    }

    // ============================================
    // DTOs DE AUTENTICACIÓN DE DOCENTE
    // ============================================

    /// <summary>
    /// DTO para login de docente
    /// </summary>
    public class LoginDocenteDto
    {
        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de respuesta de autenticación
    /// </summary>
    public class AuthDocenteResponseDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiracion { get; set; }
    }

    // ============================================
    // DTOs DE GESTIÓN DE CONTRASEÑAS
    // ============================================

    /// <summary>
    /// DTO para crear un nuevo docente con contraseña
    /// </summary>
    public class CrearDocenteConPasswordDto
    {
        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "La profesión no puede exceder 100 caracteres")]
        public string? Profesion { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "El email del usuario es requerido")]
        [EmailAddress(ErrorMessage = "El email del usuario no es válido")]
        [StringLength(100, ErrorMessage = "El email del usuario no puede exceder 100 caracteres")]
        public string EmailUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [StringLength(50, ErrorMessage = "La contraseña no puede exceder 50 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para asignar o actualizar contraseña de un docente
    /// </summary>
    public class AsignarPasswordDto
    {
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [StringLength(50, ErrorMessage = "La contraseña no puede exceder 50 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para actualizar información de un docente (sin contraseña)
    /// </summary>
    public class ActualizarDocenteDto
    {
        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son requeridos")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden exceder 100 caracteres")]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "La profesión no puede exceder 100 caracteres")]
        public string? Profesion { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        public string? Correo { get; set; }
    }

    // ============================================
    // DTOs DE CURSOS DEL DOCENTE
    // ============================================

    // DTO para curso del docente
    public class CursoDocenteDto
    {
        public int Id { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public int Ciclo { get; set; }
        public int TotalEstudiantes { get; set; }
        public decimal PromedioGeneral { get; set; }
        public decimal PorcentajeAsistenciaPromedio { get; set; }
        public int PeriodoActualId { get; set; }
        public string? PeriodoNombre { get; set; }
    }

    // DTO para estudiante en un curso
    public class EstudianteCursoDto
    {
        public int Id { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public int IdMatricula { get; set; }
        public string EstadoMatricula { get; set; } = string.Empty;
        public decimal? PromedioFinal { get; set; }
        public decimal? PorcentajeAsistencia { get; set; }
        public object? Notas { get; set; }  // Objeto dinámico con las notas
    }

    // DTO para detalle de notas
    public class NotasDetalleDto
    {
        public decimal? Parcial1 { get; set; }  // 10%
        public decimal? Parcial2 { get; set; }  // 10%
        public decimal? Practicas { get; set; }  // 20%
        public decimal? MedioCurso { get; set; }  // 20%
        public decimal? ExamenFinal { get; set; }  // 20%
        public decimal? Actitud { get; set; }  // 5%
        public decimal? Trabajos { get; set; }  // 15%
        public decimal? PromedioCalculado { get; set; }
        public decimal? PromedioFinal { get; set; }  // Con redondeo (>= 10.5 => 11)
    }

    // DTO para registrar/actualizar notas
    public class RegistrarNotasDto
    {
        [Required(ErrorMessage = "El ID de matrícula es requerido")]
        public int IdMatricula { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? Parcial1 { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? Parcial2 { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? Practicas { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? MedioCurso { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? ExamenFinal { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? Actitud { get; set; }

        [Range(0, 20, ErrorMessage = "La nota debe estar entre 0 y 20")]
        public decimal? Trabajos { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    // ============================================
    // DTOs EXTENDIDOS PARA ASISTENCIAS
    // ============================================

    /// <summary>
    /// DTO para resumen de asistencias de un estudiante en un curso específico
    /// </summary>
    public class ResumenAsistenciaEstudianteDto
    {
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int TotalClases { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
        public List<AsistenciaDetalleDto> Detalles { get; set; } = new();
    }

    /// <summary>
    /// DTO para detalle de una asistencia en el resumen
    /// </summary>
    public class AsistenciaDetalleDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public bool Presente { get; set; }
        public string TipoClase { get; set; } = string.Empty; // "Teoría" o "Práctica"
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para resumen de asistencias de todo un curso
    /// </summary>
    public class ResumenAsistenciaCursoDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int TotalEstudiantes { get; set; }
        public int TotalClases { get; set; }
        public decimal PorcentajeAsistenciaPromedio { get; set; }
        public List<ResumenAsistenciaEstudianteSimpleDto> Estudiantes { get; set; } = new();
    }

    /// <summary>
    /// DTO simplificado de resumen de asistencia de un estudiante
    /// </summary>
    public class ResumenAsistenciaEstudianteSimpleDto
    {
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public int TotalClases { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
    }

    /// <summary>
    /// DTO para obtener asistencias de un estudiante agrupadas por curso
    /// </summary>
    public class AsistenciasPorCursoDto
    {
        public int IdCurso { get; set; }
        public string CodigoCurso { get; set; } = string.Empty;
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public string? NombreDocente { get; set; }
        public int TotalClases { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
        public bool AlertaBajaAsistencia { get; set; } // true si está por debajo del 70%
        public List<AsistenciaDetalleDto> Asistencias { get; set; } = new();
    }

    /// <summary>
    /// DTO para el historial de asistencias con filtros
    /// </summary>
    public class HistorialAsistenciasDto
    {
        public List<AsistenciaDto> Asistencias { get; set; } = new();
        public int TotalRegistros { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
    }

    /// <summary>
    /// DTO para filtros de búsqueda de asistencias
    /// </summary>
    public class FiltrosAsistenciaDto
    {
        public int? IdEstudiante { get; set; }
        public int? IdCurso { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool? Presente { get; set; }
        public int? IdPeriodo { get; set; }
    }

    /// <summary>
    /// DTO para exportar reporte de asistencias
    /// </summary>
    public class ReporteAsistenciaDto
    {
        public string NombreCurso { get; set; } = string.Empty;
        public string? NombreDocente { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalClases { get; set; }
        public decimal PorcentajeAsistenciaPromedio { get; set; }
        public List<ReporteAsistenciaEstudianteDto> Estudiantes { get; set; } = new();
        public List<ReporteFechaClaseDto> Fechas { get; set; } = new();
    }

    /// <summary>
    /// DTO para datos de un estudiante en el reporte
    /// </summary>
    public class ReporteAsistenciaEstudianteDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
        public Dictionary<DateTime, bool> AsistenciasPorFecha { get; set; } = new();
    }

    /// <summary>
    /// DTO para fechas de clase en el reporte
    /// </summary>
    public class ReporteFechaClaseDto
    {
        public DateTime Fecha { get; set; }
        public int TotalPresentes { get; set; }
        public int TotalAusentes { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
    }

    /// <summary>
    /// DTO para estadísticas de asistencia de un estudiante (dashboard)
    /// </summary>
    public class EstadisticasAsistenciaEstudianteDto
    {
        public int TotalCursos { get; set; }
        public int TotalClases { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalFaltas { get; set; }
        public decimal PorcentajeAsistenciaGeneral { get; set; }
        public int CursosConAlerta { get; set; } // Cursos con asistencia < 70%
        public List<AsistenciasPorCursoDto> CursosPorAsistencia { get; set; } = new();
    }

    /// <summary>
    /// DTO para tendencia de asistencia por mes
    /// </summary>
    public class TendenciaAsistenciaDto
    {
        public string Mes { get; set; } = string.Empty; // Formato: "Enero 2024"
        public int Anio { get; set; }
        public int NumeroMes { get; set; }
        public int TotalClases { get; set; }
        public int TotalAsistencias { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
    }
}
