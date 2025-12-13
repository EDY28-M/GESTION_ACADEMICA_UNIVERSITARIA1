using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs DE MATRÍCULA
    // ============================================

    /// <summary>
    /// DTO para mostrar información de una matrícula
    /// </summary>
    public class MatriculaDto
    {
        public int Id { get; set; }
        public int IdEstudiante { get; set; }
        public int IdCurso { get; set; }
        public string CodigoCurso { get; set; } = string.Empty;
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public string? NombreDocente { get; set; }
        public int IdPeriodo { get; set; }
        public string NombrePeriodo { get; set; } = string.Empty;
        public DateTime FechaMatricula { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaRetiro { get; set; }
        public decimal? PromedioFinal { get; set; }
    }

    /// <summary>
    /// DTO para realizar una matrícula individual
    /// </summary>
    public class MatricularDto
    {
        [Required]
        public int IdCurso { get; set; }

        [Required]
        public int IdPeriodo { get; set; }
    }

    /// <summary>
    /// DTO para matrícula dirigida (administrativa) de múltiples estudiantes
    /// </summary>
    public class MatriculaDirigidaDto
    {
        public int IdCurso { get; set; }
        public List<int> IdsEstudiantes { get; set; } = new List<int>();
        public int IdPeriodo { get; set; }
    }
}
