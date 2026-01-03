using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs PARA TRABAJOS ENCARGADOS
    // ============================================

    /// <summary>
    /// DTO para crear un nuevo trabajo encargado
    /// </summary>
    public class TrabajoCreateDto
    {
        [Required]
        public int IdCurso { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required]
        public DateTime FechaLimite { get; set; }

        public int? IdTipoEvaluacion { get; set; }

        public List<ArchivoDto>? Archivos { get; set; }
        public List<LinkDto>? Links { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un trabajo encargado
    /// </summary>
    public class TrabajoUpdateDto
    {
        [StringLength(200)]
        public string? Titulo { get; set; }

        public string? Descripcion { get; set; }

        public DateTime? FechaLimite { get; set; }

        public bool? Activo { get; set; }

        public int? IdTipoEvaluacion { get; set; }

        public List<ArchivoDto>? ArchivosNuevos { get; set; }
        public List<LinkDto>? LinksNuevos { get; set; }
        public List<int>? ArchivosEliminar { get; set; }
        public List<int>? LinksEliminar { get; set; }
    }

    /// <summary>
    /// DTO completo de trabajo encargado
    /// </summary>
    public class TrabajoDto
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string? NombreCurso { get; set; }
        public int IdDocente { get; set; }
        public string? NombreDocente { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaLimite { get; set; }
        public bool Activo { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public List<ArchivoDto> Archivos { get; set; } = new List<ArchivoDto>();
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
        public int TotalEntregas { get; set; }
        public bool PuedeEntregar { get; set; }
        public bool YaEntregado { get; set; }
        
        // Información del tipo de evaluación
        public int? IdTipoEvaluacion { get; set; }
        public string? NombreTipoEvaluacion { get; set; }
        public decimal? PesoTipoEvaluacion { get; set; }
        
        // Información de la entrega del estudiante (si ya entregó)
        public decimal? Calificacion { get; set; }
        public string? ObservacionesDocente { get; set; }
        public DateTime? FechaCalificacion { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }

    /// <summary>
    /// DTO simplificado de trabajo para listados
    /// </summary>
    public class TrabajoSimpleDto
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string? NombreCurso { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaLimite { get; set; }
        public bool Activo { get; set; }
        public int TotalEntregas { get; set; }
        public bool YaEntregado { get; set; }
        public int? IdTipoEvaluacion { get; set; }
        public string? NombreTipoEvaluacion { get; set; }
    }

    /// <summary>
    /// DTO para archivo adjunto
    /// </summary>
    public class ArchivoDto
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public string? TipoArchivo { get; set; }
        public long? Tamaño { get; set; }
        public DateTime FechaSubida { get; set; }
    }

    /// <summary>
    /// DTO para link
    /// </summary>
    public class LinkDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    // ============================================
    // DTOs PARA ENTREGAS DE ESTUDIANTES
    // ============================================

    /// <summary>
    /// DTO para crear una entrega de trabajo
    /// </summary>
    public class EntregaCreateDto
    {
        [Required]
        public int IdTrabajo { get; set; }

        public string? Comentario { get; set; }

        public List<ArchivoDto>? Archivos { get; set; }
        public List<LinkDto>? Links { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una entrega
    /// </summary>
    public class EntregaUpdateDto
    {
        public string? Comentario { get; set; }
        public List<ArchivoDto>? ArchivosNuevos { get; set; }
        public List<LinkDto>? LinksNuevos { get; set; }
        public List<int>? ArchivosEliminar { get; set; }
        public List<int>? LinksEliminar { get; set; }
    }

    /// <summary>
    /// DTO completo de entrega
    /// </summary>
    public class EntregaDto
    {
        public int Id { get; set; }
        public int IdTrabajo { get; set; }
        public string? TituloTrabajo { get; set; }
        public int IdEstudiante { get; set; }
        public string? NombreEstudiante { get; set; }
        public string? CodigoEstudiante { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaEntrega { get; set; }
        public decimal? Calificacion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime? FechaCalificacion { get; set; }
        public bool EntregadoTarde { get; set; }
        public List<ArchivoDto> Archivos { get; set; } = new List<ArchivoDto>();
        public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }

    /// <summary>
    /// DTO para calificar una entrega
    /// </summary>
    public class CalificarEntregaDto
    {
        [Required]
        [Range(0, 20, ErrorMessage = "La calificación debe estar entre 0 y 20")]
        public decimal Calificacion { get; set; }

        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para trabajos pendientes en el dashboard del docente
    /// </summary>
    public class TrabajoPendienteDto
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string? NombreCurso { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaLimite { get; set; }
        public int TotalEntregas { get; set; }
        public int EntregasPendientesCalificar { get; set; }
        public DateTime? FechaUltimaEntrega { get; set; }
    }
}

