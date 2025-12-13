using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using System.Security.Claims;
using API_REST_CURSOSACADEMICOS.Extensions;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocentesController : ControllerBase
    {
        private readonly IDocentesService _docentesService;
        private readonly ILogger<DocentesController> _logger;

        public DocentesController(IDocentesService docentesService, ILogger<DocentesController> logger)
        {
            _docentesService = docentesService;
            _logger = logger;
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
        /// Obtiene el ID del docente autenticado
        /// </summary>
        private int? ObtenerIdDocente()
        {
            return User.TryGetDocenteId(out var docenteId) ? docenteId : null;
        }

        private IActionResult FromOutcome(ServiceOutcome outcome)
        {
            return outcome.Status switch
            {
                ServiceOutcomeStatus.Ok => Ok(outcome.Payload),
                ServiceOutcomeStatus.BadRequest => BadRequest(outcome.Payload),
                ServiceOutcomeStatus.NotFound => NotFound(outcome.Payload),
                ServiceOutcomeStatus.Unauthorized => Unauthorized(outcome.Payload),
                ServiceOutcomeStatus.Forbidden => Forbid(),
                _ => StatusCode(500, new { message = "Error interno del servidor" })
            };
        }

        // GET: api/Docentes
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<DocenteDto>>> GetDocentes()
        {
            var docentes = await _docentesService.GetDocentesAsync();
            return Ok(docentes);
        }

        // GET: api/Docentes/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<DocenteDto>> GetDocente(int id)
        {
            var docente = await _docentesService.GetDocenteAsync(id);
            if (docente == null) return NotFound($"Docente con ID {id} no encontrado");
            return Ok(docente);
        }

        // POST: api/Docentes
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<DocenteDto>> PostDocente(DocenteCreateDto docenteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error, created) = await _docentesService.CreateDocenteAsync(docenteDto);
            if (!success)
            {
                return BadRequest(error);
            }

            return CreatedAtAction(nameof(GetDocente), new { id = created!.Id }, created);
        }

        // PUT: api/Docentes/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutDocente(int id, DocenteUpdateDto docenteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var (notFound, success, error) = await _docentesService.UpdateDocenteAsync(id, docenteDto);
                if (notFound) return NotFound(error);
                if (!success) return BadRequest(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar docente");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }

            return NoContent();
        }

        // DELETE: api/Docentes/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteDocente(int id)
        {
            var (notFound, hasAssignedCourses, success, error) = await _docentesService.DeleteDocenteAsync(id);
            if (notFound) return NotFound(error);
            if (hasAssignedCourses) return BadRequest(error);
            if (!success) return StatusCode(500, new { message = "Error interno del servidor" });

            return NoContent();
        }

        #region Endpoints para Docente Autenticado

        /// <summary>
        /// GET /api/docentes/mis-cursos - Obtiene los cursos del docente autenticado
        /// OPTIMIZADO: Consultas eficientes sin N+1
        /// </summary>
        [HttpGet("mis-cursos")]
        [Authorize]
        public async Task<IActionResult> GetMisCursos()
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var cursosDto = await _docentesService.GetMisCursosAsync(docenteId.Value);
                return Ok(cursosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cursos del docente");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{id}/estudiantes - Obtiene los estudiantes de un curso
        /// OPTIMIZADO: Sin consultas N+1
        /// </summary>
        [HttpGet("cursos/{idCurso}/estudiantes")]
        [Authorize]
        public async Task<IActionResult> GetEstudiantesCurso(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.GetEstudiantesCursoAsync(docenteId.Value, idCurso);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estudiantes del curso");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/cursos/{idCurso}/notas - Registrar o actualizar notas de un estudiante
        /// </summary>
        [HttpPost("cursos/{idCurso}/notas")]
        [Authorize]
        public async Task<IActionResult> RegistrarNotas(int idCurso, [FromBody] System.Text.Json.JsonElement notasJson)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.RegistrarNotasAsync(docenteId.Value, idCurso, notasJson);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar notas");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/tipos-evaluacion - Obtener tipos de evaluación configurados para un curso
        /// </summary>
        [HttpGet("cursos/{idCurso}/tipos-evaluacion")]
        [Authorize]
        public async Task<IActionResult> ObtenerTiposEvaluacion(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.ObtenerTiposEvaluacionAsync(docenteId.Value, idCurso);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de evaluación");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/cursos/{idCurso}/tipos-evaluacion - Configurar tipos de evaluación para un curso
        /// </summary>
        [HttpPost("cursos/{idCurso}/tipos-evaluacion")]
        [Authorize]
        public async Task<IActionResult> ConfigurarTiposEvaluacion(int idCurso, [FromBody] ConfigurarTiposEvaluacionDto configDto)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.ConfigurarTiposEvaluacionAsync(docenteId.Value, idCurso, configDto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar tipos de evaluación");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/asistencia - Registrar asistencia masiva para una fecha
        /// </summary>
        [HttpPost("asistencia")]
        [Authorize]
        public async Task<IActionResult> RegistrarAsistencia([FromBody] RegistrarAsistenciasMasivasDto asistenciaDto)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.RegistrarAsistenciaAsync(docenteId.Value, asistenciaDto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencias");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/asistencia - Obtener resumen de asistencia del curso
        /// </summary>
        [HttpGet("cursos/{idCurso}/asistencia")]
        [Authorize]
        public async Task<IActionResult> GetAsistenciaCurso(int idCurso, [FromQuery] DateTime? fecha = null, [FromQuery] string? tipoClase = null)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.GetAsistenciaCursoAsync(docenteId.Value, idCurso, fecha, tipoClase);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias del curso");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/asistencia/resumen - Obtener resumen de asistencia por estudiante
        /// </summary>
        [HttpGet("cursos/{idCurso}/asistencia/resumen")]
        [Authorize]
        public async Task<IActionResult> GetResumenAsistencia(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                var outcome = await _docentesService.GetResumenAsistenciaAsync(docenteId.Value, idCurso);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencias");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        #endregion

        // Los métodos auxiliares (notas/normalización) se movieron a `DocentesService` (Infrastructure).
    }
}
