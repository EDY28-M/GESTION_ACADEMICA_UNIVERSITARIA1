using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces
{
    /// <summary>
    /// Interfaz del servicio de asistencias siguiendo el principio de Inversión de Dependencias (SOLID)
    /// Define los contratos para la gestión de asistencias de estudiantes
    /// </summary>
    public interface IAsistenciaService
    {
        // ============================================
        // MÉTODOS PARA DOCENTES
        // ============================================

        /// <summary>
        /// Registra una asistencia individual para un estudiante
        /// </summary>
        Task<AsistenciaDto> RegistrarAsistenciaAsync(RegistrarAsistenciaDto dto);

        /// <summary>
        /// Registra múltiples asistencias para un curso en una fecha específica
        /// Ideal para tomar asistencia de toda una clase
        /// </summary>
        Task<List<AsistenciaDto>> RegistrarAsistenciasMasivasAsync(RegistrarAsistenciasMasivasDto dto);

        /// <summary>
        /// Actualiza una asistencia existente
        /// </summary>
        Task<AsistenciaDto> ActualizarAsistenciaAsync(int idAsistencia, ActualizarAsistenciaDto dto);

        /// <summary>
        /// Elimina una asistencia por su ID
        /// </summary>
        Task<bool> EliminarAsistenciaAsync(int idAsistencia);

        /// <summary>
        /// Obtiene el resumen de asistencias de un curso específico
        /// Incluye estadísticas de todos los estudiantes del curso
        /// </summary>
        Task<ResumenAsistenciaCursoDto> GetResumenAsistenciaCursoAsync(int idCurso, DateTime? fechaInicio = null, DateTime? fechaFin = null);

        /// <summary>
        /// Obtiene el historial de asistencias con filtros avanzados
        /// </summary>
        Task<HistorialAsistenciasDto> GetHistorialAsistenciasAsync(FiltrosAsistenciaDto filtros);

        /// <summary>
        /// Genera un reporte exportable de asistencias de un curso
        /// </summary>
        Task<ReporteAsistenciaDto> GenerarReporteAsistenciaAsync(int idCurso, DateTime? fechaInicio = null, DateTime? fechaFin = null);

        /// <summary>
        /// Obtiene las asistencias de un curso en una fecha específica
        /// Útil para ver/editar asistencias ya registradas
        /// </summary>
        Task<List<AsistenciaDto>> GetAsistenciasPorCursoYFechaAsync(int idCurso, DateTime fecha);

        // ============================================
        // MÉTODOS PARA ESTUDIANTES
        // ============================================

        /// <summary>
        /// Obtiene todas las asistencias de un estudiante agrupadas por curso
        /// Ideal para la vista principal de asistencias del estudiante
        /// </summary>
        Task<List<AsistenciasPorCursoDto>> GetAsistenciasPorEstudianteAsync(int idEstudiante, int? idPeriodo = null);

        /// <summary>
        /// Obtiene el resumen de asistencias de un estudiante en un curso específico
        /// </summary>
        Task<ResumenAsistenciaEstudianteDto> GetResumenAsistenciaEstudianteCursoAsync(int idEstudiante, int idCurso);

        /// <summary>
        /// Obtiene estadísticas generales de asistencia de un estudiante
        /// Incluye totales, promedios y alertas
        /// </summary>
        Task<EstadisticasAsistenciaEstudianteDto> GetEstadisticasAsistenciaEstudianteAsync(int idEstudiante, int? idPeriodo = null);

        /// <summary>
        /// Obtiene la tendencia de asistencia por mes para gráficos
        /// </summary>
        Task<List<TendenciaAsistenciaDto>> GetTendenciaAsistenciaEstudianteAsync(int idEstudiante, int meses = 6);

        // ============================================
        // MÉTODOS AUXILIARES
        // ============================================

        /// <summary>
        /// Verifica si ya existe una asistencia registrada para un estudiante en un curso y fecha
        /// </summary>
        Task<bool> ExisteAsistenciaAsync(int idEstudiante, int idCurso, DateTime fecha);

        /// <summary>
        /// Calcula el porcentaje de asistencia de un estudiante en un curso
        /// </summary>
        Task<decimal> CalcularPorcentajeAsistenciaAsync(int idEstudiante, int idCurso);

        /// <summary>
        /// Obtiene todas las asistencias de un estudiante
        /// </summary>
        Task<List<AsistenciaDto>> GetAsistenciasByEstudianteAsync(int idEstudiante);

        /// <summary>
        /// Obtiene todas las asistencias de un curso
        /// </summary>
        Task<List<AsistenciaDto>> GetAsistenciasByCursoAsync(int idCurso);
    }
}
