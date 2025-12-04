namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs DE NOTAS Y EVALUACIONES
    // ============================================

    /// <summary>
    /// DTO para mostrar información de una nota individual
    /// </summary>
    public class NotaDto
    {
        public int Id { get; set; }
        public int IdMatricula { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public string NombrePeriodo { get; set; } = string.Empty;
        public string TipoEvaluacion { get; set; } = string.Empty;
        public decimal NotaValor { get; set; }
        public decimal Peso { get; set; }
        public DateTime Fecha { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para el registro consolidado de notas por semestres
    /// </summary>
    public class RegistroNotasDto
    {
        public List<SemestreRegistroDto> Semestres { get; set; } = new();
    }

    /// <summary>
    /// DTO para información de un semestre en el registro de notas
    /// </summary>
    public class SemestreRegistroDto
    {
        public int IdPeriodo { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Ciclo { get; set; } = string.Empty;  // 'I' o 'II' - Semestre del período
        public int CicloAcademico { get; set; }  // 1, 2, 3... 10 - Ciclo académico del estudiante
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<CursoRegistroDto> Cursos { get; set; } = new();
        public TotalesSemestreDto Totales { get; set; } = new();
    }

    /// <summary>
    /// DTO para información de un curso en el registro de notas
    /// </summary>
    public class CursoRegistroDto
    {
        public int IdMatricula { get; set; }
        public int IdCurso { get; set; }
        public string CodigoCurso { get; set; } = string.Empty;
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public DateTime? FechaExamen { get; set; }
        public int NotaFinal { get; set; }
        public string EstadoCurso { get; set; } = string.Empty;
        public List<EvaluacionRegistroDto> Evaluaciones { get; set; } = new();
    }

    /// <summary>
    /// DTO para información de una evaluación en el registro
    /// </summary>
    public class EvaluacionRegistroDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int Peso { get; set; }
        public decimal Nota { get; set; }
    }

    /// <summary>
    /// DTO para totales de un semestre
    /// </summary>
    public class TotalesSemestreDto
    {
        public int TotalCreditos { get; set; }
        public int TotalHoras { get; set; }
        public decimal PromedioSemestral { get; set; }
        public decimal PromedioAcumulado { get; set; }
    }
}
