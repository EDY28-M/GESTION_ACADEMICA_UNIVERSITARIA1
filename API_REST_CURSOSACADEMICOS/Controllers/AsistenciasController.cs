using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    /// <summary>
    /// Controlador para la gestión de asistencias de estudiantes
    /// Proporciona endpoints tanto para docentes (registro y consultas)
    /// como para estudiantes (visualización de sus asistencias)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AsistenciasController : ControllerBase
    {
        private readonly IAsistenciaService _asistenciaService;
        private readonly ILogger<AsistenciasController> _logger;

        public AsistenciasController(
            IAsistenciaService asistenciaService,
            ILogger<AsistenciasController> logger)
        {
            _asistenciaService = asistenciaService;
            _logger = logger;
        }

        // ============================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ============================================

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde el token JWT
        /// </summary>
        private int GetUsuarioId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return int.Parse(userIdClaim);
        }

        /// <summary>
        /// Verifica si el usuario autenticado es un docente
        /// </summary>
        private bool EsDocente()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "Docente";
        }

        /// <summary>
        /// Verifica si el usuario autenticado es un estudiante
        /// </summary>
        private bool EsEstudiante()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "Estudiante";
        }

        // ============================================
        // ENDPOINTS PARA DOCENTES
        // ============================================

        /// <summary>
        /// POST /api/asistencias/registrar
        /// Registra una asistencia individual para un estudiante
        /// </summary>
        [HttpPost("registrar")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<AsistenciaDto>> RegistrarAsistencia([FromBody] RegistrarAsistenciaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asistencia = await _asistenciaService.RegistrarAsistenciaAsync(dto);
                return Ok(asistencia);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencia");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/asistencias/registrar-masivas
        /// Registra asistencias para múltiples estudiantes de un curso en una fecha
        /// Ideal para tomar asistencia de toda una clase
        /// </summary>
        [HttpPost("registrar-masivas")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<List<AsistenciaDto>>> RegistrarAsistenciasMasivas(
            [FromBody] RegistrarAsistenciasMasivasDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asistencias = await _asistenciaService.RegistrarAsistenciasMasivasAsync(dto);
                return Ok(new
                {
                    mensaje = $"Se registraron {asistencias.Count} asistencias exitosamente",
                    asistencias
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencias masivas");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/asistencias/{id}
        /// Actualiza una asistencia existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<AsistenciaDto>> ActualizarAsistencia(
            int id,
            [FromBody] ActualizarAsistenciaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var asistencia = await _asistenciaService.ActualizarAsistenciaAsync(id, dto);
                return Ok(asistencia);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar asistencia");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/asistencias/{id}
        /// Elimina una asistencia por su ID
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult> EliminarAsistencia(int id)
        {
            try
            {
                var eliminado = await _asistenciaService.EliminarAsistenciaAsync(id);
                if (!eliminado)
                    return NotFound(new { mensaje = $"Asistencia con ID {id} no encontrada" });

                return Ok(new { mensaje = "Asistencia eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar asistencia");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/curso/{idCurso}/resumen
        /// Obtiene el resumen de asistencias de un curso completo
        /// Incluye estadísticas de todos los estudiantes
        /// </summary>
        [HttpGet("curso/{idCurso}/resumen")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<ResumenAsistenciaCursoDto>> GetResumenAsistenciaCurso(
            int idCurso,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var resumen = await _asistenciaService.GetResumenAsistenciaCursoAsync(idCurso, fechaInicio, fechaFin);
                return Ok(resumen);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencias del curso");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/curso/{idCurso}/fecha/{fecha}
        /// Obtiene las asistencias de un curso en una fecha específica
        /// Útil para ver o editar asistencias ya registradas
        /// </summary>
        [HttpGet("curso/{idCurso}/fecha/{fecha}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<List<AsistenciaDto>>> GetAsistenciasPorCursoYFecha(
            int idCurso,
            DateTime fecha)
        {
            try
            {
                var asistencias = await _asistenciaService.GetAsistenciasPorCursoYFechaAsync(idCurso, fecha);
                return Ok(asistencias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias por curso y fecha");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/curso/{idCurso}/reporte
        /// Genera un reporte completo de asistencias del curso
        /// Incluye datos para exportación
        /// </summary>
        [HttpGet("curso/{idCurso}/reporte")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<ReporteAsistenciaDto>> GenerarReporteAsistencia(
            int idCurso,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var reporte = await _asistenciaService.GenerarReporteAsistenciaAsync(idCurso, fechaInicio, fechaFin);
                return Ok(reporte);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de asistencias");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/historial
        /// Obtiene el historial de asistencias con filtros avanzados
        /// </summary>
        [HttpGet("historial")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<HistorialAsistenciasDto>> GetHistorialAsistencias(
            [FromQuery] int? idEstudiante = null,
            [FromQuery] int? idCurso = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] bool? presente = null,
            [FromQuery] int? idPeriodo = null)
        {
            try
            {
                var filtros = new FiltrosAsistenciaDto
                {
                    IdEstudiante = idEstudiante,
                    IdCurso = idCurso,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Presente = presente,
                    IdPeriodo = idPeriodo
                };

                var historial = await _asistenciaService.GetHistorialAsistenciasAsync(filtros);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de asistencias");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ============================================
        // ENDPOINTS PARA ESTUDIANTES
        // ============================================

        /// <summary>
        /// GET /api/asistencias/mis-asistencias
        /// Obtiene todas las asistencias del estudiante autenticado agrupadas por curso
        /// Endpoint principal para la vista de asistencias del estudiante
        /// </summary>
        [HttpGet("mis-asistencias")]
        [Authorize]
        public ActionResult<List<AsistenciasPorCursoDto>> GetMisAsistencias(
            [FromQuery] int? idPeriodo = null)
        {
            try
            {
                // Este endpoint es solo para estudiantes
                if (!EsEstudiante())
                    return Forbid();

                // Este endpoint no se usa directamente ya que el frontend llama a /estudiante/{id}
                // Redirigir a ese endpoint sería redundante
                return BadRequest(new { mensaje = "Use el endpoint /api/asistencias/estudiante/{idEstudiante}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias del estudiante");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/estudiante/{idEstudiante}
        /// Obtiene todas las asistencias de un estudiante específico
        /// Accesible por docentes y administradores
        /// </summary>
        [HttpGet("estudiante/{idEstudiante}")]
        [Authorize]
        public async Task<ActionResult<List<AsistenciasPorCursoDto>>> GetAsistenciasEstudiante(
            int idEstudiante,
            [FromQuery] int? idPeriodo = null)
        {
            try
            {
                var asistencias = await _asistenciaService.GetAsistenciasPorEstudianteAsync(idEstudiante, idPeriodo);
                return Ok(asistencias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias del estudiante");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/estudiante/{idEstudiante}/curso/{idCurso}
        /// Obtiene el resumen de asistencias de un estudiante en un curso específico
        /// </summary>
        [HttpGet("estudiante/{idEstudiante}/curso/{idCurso}")]
        [Authorize]
        public async Task<ActionResult<ResumenAsistenciaEstudianteDto>> GetResumenAsistenciaEstudianteCurso(
            int idEstudiante,
            int idCurso)
        {
            try
            {
                var resumen = await _asistenciaService.GetResumenAsistenciaEstudianteCursoAsync(idEstudiante, idCurso);
                return Ok(resumen);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencia estudiante-curso");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/estudiante/{idEstudiante}/estadisticas
        /// Obtiene estadísticas completas de asistencia de un estudiante
        /// Incluye totales, promedios y alertas
        /// </summary>
        [HttpGet("estudiante/{idEstudiante}/estadisticas")]
        [Authorize]
        public async Task<ActionResult<EstadisticasAsistenciaEstudianteDto>> GetEstadisticasAsistenciaEstudiante(
            int idEstudiante,
            [FromQuery] int? idPeriodo = null)
        {
            try
            {
                var estadisticas = await _asistenciaService.GetEstadisticasAsistenciaEstudianteAsync(idEstudiante, idPeriodo);
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de asistencia del estudiante");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/estudiante/{idEstudiante}/tendencia
        /// Obtiene la tendencia de asistencia por mes para gráficos
        /// </summary>
        [HttpGet("estudiante/{idEstudiante}/tendencia")]
        [Authorize]
        public async Task<ActionResult<List<TendenciaAsistenciaDto>>> GetTendenciaAsistenciaEstudiante(
            int idEstudiante,
            [FromQuery] int meses = 6)
        {
            try
            {
                var tendencia = await _asistenciaService.GetTendenciaAsistenciaEstudianteAsync(idEstudiante, meses);
                return Ok(tendencia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tendencia de asistencia del estudiante");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        // ============================================
        // ENDPOINTS AUXILIARES
        // ============================================

        /// <summary>
        /// GET /api/asistencias/existe
        /// Verifica si ya existe una asistencia registrada
        /// </summary>
        [HttpGet("existe")]
        [Authorize]
        public async Task<ActionResult<bool>> ExisteAsistencia(
            [FromQuery] int idEstudiante,
            [FromQuery] int idCurso,
            [FromQuery] DateTime fecha)
        {
            try
            {
                var existe = await _asistenciaService.ExisteAsistenciaAsync(idEstudiante, idCurso, fecha);
                return Ok(new { existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de asistencia");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/asistencias/porcentaje
        /// Calcula el porcentaje de asistencia de un estudiante en un curso
        /// </summary>
        [HttpGet("porcentaje")]
        [Authorize]
        public async Task<ActionResult<decimal>> CalcularPorcentajeAsistencia(
            [FromQuery] int idEstudiante,
            [FromQuery] int idCurso)
        {
            try
            {
                var porcentaje = await _asistenciaService.CalcularPorcentajeAsistenciaAsync(idEstudiante, idCurso);
                return Ok(new { porcentaje });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular porcentaje de asistencia");
                return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}
