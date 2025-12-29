using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    /// <summary>
    /// Controlador exclusivo para operaciones administrativas
    /// Requiere rol de Administrador para todos los endpoints
    /// </summary>
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
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
                _ => StatusCode(500, new { mensaje = "Error interno del servidor" })
            };
        }

        [HttpGet("estudiantes")]
        public async Task<IActionResult> GetTodosEstudiantes()
        {
            try
            {
                var outcome = await _adminService.GetTodosEstudiantesAsync();
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener estudiantes", detalle = ex.Message });
            }
        }

        [HttpGet("estudiantes/{id}/detalle")]
        public async Task<IActionResult> GetEstudianteDetalle(int id)
        {
            try
            {
                var outcome = await _adminService.GetEstudianteDetalleAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener detalle del estudiante", detalle = ex.Message });
            }
        }

        [HttpPost("estudiantes")]
        public async Task<IActionResult> CrearEstudiante([FromBody] CrearEstudianteDto dto)
        {
            try
            {
                var outcome = await _adminService.CrearEstudianteAsync(dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear estudiante", detalle = ex.Message });
            }
        }

        [HttpDelete("estudiantes/{id}")]
        public async Task<IActionResult> EliminarEstudiante(int id)
        {
            try
            {
                var outcome = await _adminService.EliminarEstudianteAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar estudiante", detalle = ex.Message });
            }
        }

        [HttpPost("cursos-dirigidos")]
        public async Task<IActionResult> CrearCursosDirigidos([FromBody] MatriculaDirigidaDto dto)
        {
            try
            {
                var outcome = await _adminService.CrearCursosDirigidosAsync(dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear cursos dirigidos", detalle = ex.Message });
            }
        }

        [HttpGet("periodos")]
        public async Task<IActionResult> GetPeriodos()
        {
            try
            {
                var outcome = await _adminService.GetPeriodosAsync();
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener períodos", detalle = ex.Message });
            }
        }

        [HttpPost("periodos")]
        public async Task<IActionResult> CrearPeriodo([FromBody] CrearPeriodoDto dto)
        {
            try
            {
                var outcome = await _adminService.CrearPeriodoAsync(dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear período", detalle = ex.Message });
            }
        }

        [HttpPut("periodos/{id}")]
        public async Task<IActionResult> EditarPeriodo(int id, [FromBody] EditarPeriodoDto dto)
        {
            try
            {
                var outcome = await _adminService.EditarPeriodoAsync(id, dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar período", detalle = ex.Message });
            }
        }

        [HttpPut("periodos/{id}/activar")]
        public async Task<IActionResult> ActivarPeriodo(int id)
        {
            try
            {
                var outcome = await _adminService.ActivarPeriodoAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al activar período", detalle = ex.Message });
            }
        }

        [HttpDelete("periodos/{id}")]
        public async Task<IActionResult> EliminarPeriodo(int id)
        {
            try
            {
                var outcome = await _adminService.EliminarPeriodoAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar período", detalle = ex.Message });
            }
        }

        [HttpGet("docentes")]
        public async Task<IActionResult> GetTodosDocentes()
        {
            try
            {
                var outcome = await _adminService.GetTodosDocentesAsync();
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener docentes", detalle = ex.Message });
            }
        }

        [HttpPost("docentes")]
        public async Task<IActionResult> CrearDocente([FromBody] CrearDocenteConPasswordDto dto)
        {
            try
            {
                var outcome = await _adminService.CrearDocenteAsync(dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear docente", detalle = ex.Message });
            }
        }

        [HttpPut("docentes/{id}/password")]
        public async Task<IActionResult> AsignarPasswordDocente(int id, [FromBody] AsignarPasswordDto dto)
        {
            try
            {
                var outcome = await _adminService.AsignarPasswordDocenteAsync(id, dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al asignar contraseña", detalle = ex.Message });
            }
        }

        [HttpPut("docentes/{id}")]
        public async Task<IActionResult> ActualizarDocente(int id, [FromBody] ActualizarDocenteDto dto)
        {
            try
            {
                var outcome = await _adminService.ActualizarDocenteAsync(id, dto);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar docente", detalle = ex.Message });
            }
        }

        [HttpDelete("docentes/{id}")]
        public async Task<IActionResult> EliminarDocente(int id)
        {
            try
            {
                var outcome = await _adminService.EliminarDocenteAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar docente", detalle = ex.Message });
            }
        }

        [HttpPost("periodos/{id}/cerrar")]
        public async Task<IActionResult> CerrarPeriodo(int id)
        {
            try
            {
                var outcome = await _adminService.CerrarPeriodoAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cerrar período", detalle = ex.Message });
            }
        }

        [HttpPost("periodos/{id}/abrir")]
        public async Task<IActionResult> AbrirPeriodo(int id)
        {
            try
            {
                var outcome = await _adminService.AbrirPeriodoAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al abrir período", detalle = ex.Message });
            }
        }

        [HttpGet("periodos/{id}/validar-cierre")]
        public async Task<IActionResult> ValidarCierrePeriodo(int id)
        {
            try
            {
                var outcome = await _adminService.ValidarCierrePeriodoAsync(id);
                return FromOutcome(outcome);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al validar cierre de período", detalle = ex.Message });
            }
        }
    }
}


