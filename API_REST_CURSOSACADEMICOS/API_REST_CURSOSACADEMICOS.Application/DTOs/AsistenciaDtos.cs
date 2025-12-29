namespace API_REST_CURSOSACADEMICOS.DTOs
{
    /// <summary>
    /// DTO para registrar una asistencia individual
    /// </summary>
    public class RegistrarAsistenciaDto
    {
        public int IdEstudiante { get; set; }
        public int IdCurso { get; set; }
        public DateTime Fecha { get; set; }
        public bool Presente { get; set; }
        public string? Observaciones { get; set; }
        public string TipoClase { get; set; } = "Teoría"; // "Teoría" o "Práctica"
    }

    /// <summary>
    /// DTO para registrar asistencias masivas (múltiples estudiantes)
    /// </summary>
    public class RegistrarAsistenciasMasivasDto
    {
        public int IdCurso { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoClase { get; set; } = "Teoría";
        public List<AsistenciaEstudianteDto> Estudiantes { get; set; } = new();
    }

    public class AsistenciaEstudianteDto
    {
        public int IdEstudiante { get; set; }
        public bool Presente { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO para actualizar una asistencia existente
    /// </summary>
    public class ActualizarAsistenciaDto
    {
        public bool Presente { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// DTO de respuesta con información de asistencia
    /// </summary>
    public class AsistenciaDto
    {
        public int Id { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public bool Presente { get; set; }
        public string? Observaciones { get; set; }
        public string TipoClase { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    /// <summary>
    /// DTO con estadísticas de asistencia de un estudiante en un curso
    /// </summary>
    public class EstadisticasAsistenciaDto
    {
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        
        // Sesiones esperadas según créditos
        public int SesionesPorSemana { get; set; }
        public int TotalSesionesEsperadas { get; set; }
        
        // Asistencias registradas
        public int TotalAsistencias { get; set; }
        public int AsistenciasPresente { get; set; }
        public int AsistenciasFalta { get; set; }
        
        // Porcentajes
        public decimal PorcentajeAsistencia { get; set; }
        public decimal PorcentajeInasistencia { get; set; }
        
        // Control de examen final
        public bool PuedeDarExamenFinal { get; set; }
        public string MensajeBloqueo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para respuesta con información extendida de asistencia (incluye estadísticas)
    /// </summary>
    public class AsistenciaConEstadisticasDto : AsistenciaDto
    {
        public EstadisticasAsistenciaDto? Estadisticas { get; set; }
    }

    /// <summary>
    /// DTO con resumen de asistencias de un estudiante en un curso
    /// </summary>
    public class ResumenAsistenciaDto
    {
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = string.Empty;
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int TotalAsistencias { get; set; }
        public int AsistenciasPresente { get; set; }
        public int AsistenciasFalta { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
        public List<AsistenciaDto> Asistencias { get; set; } = new();
    }
}
