using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosController : ControllerBase
    {
        private readonly IHorarioService _horarioService;
        private readonly IUserLookupService _userLookupService;

        public HorariosController(IHorarioService horarioService, IUserLookupService userLookupService)
        {
            _horarioService = horarioService;
            _userLookupService = userLookupService;
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<HorarioDto>> CrearHorario([FromBody] CrearHorarioDto horarioDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var horario = await _horarioService.CrearHorarioAsync(horarioDto);
                return CreatedAtAction(nameof(GetHorariosPorCurso), new { idCurso = horario.IdCurso }, horario);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al crear el horario", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarHorario(int id)
        {
            var result = await _horarioService.EliminarHorarioAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Horario no encontrado" });
            }
            return NoContent();
        }

        [HttpGet("curso/{idCurso}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<HorarioDto>>> GetHorariosPorCurso(int idCurso)
        {
            var horarios = await _horarioService.ObtenerPorCursoAsync(idCurso);
            return Ok(horarios);
        }

        [HttpGet("mi-horario")]
        [Authorize(Roles = "Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<HorarioDto>>> GetMiHorario()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(role))
            {
                return Unauthorized();
            }

            if (role == "Docente")
            {
                if (string.IsNullOrEmpty(email)) return Unauthorized();

                var docenteId = await _userLookupService.GetDocenteIdByEmailAsync(email);
                if (!docenteId.HasValue)
                {
                    return NotFound(new { message = "Perfil de docente no encontrado para este usuario." });
                }

                var horarios = await _horarioService.ObtenerPorDocenteAsync(docenteId.Value);
                return Ok(horarios);
            }
            else if (role == "Estudiante")
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                var estudianteId = await _userLookupService.GetEstudianteIdByUsuarioIdAsync(userId);
                if (!estudianteId.HasValue && !string.IsNullOrEmpty(email))
                {
                    // Fallback por email si IdUsuario no está seteado (caso legacy o error)
                    estudianteId = await _userLookupService.GetEstudianteIdByEmailAsync(email);
                }

                if (!estudianteId.HasValue)
                {
                    return NotFound(new { message = "Perfil de estudiante no encontrado para este usuario." });
                }

                var horarios = await _horarioService.ObtenerPorEstudianteAsync(estudianteId.Value);
                return Ok(horarios);
            }

            return BadRequest(new { message = "Rol no válido para esta operación." });
        }

        /// <summary>
        /// Obtiene todos los docentes con sus cursos activos y horarios asignados
        /// </summary>
        [HttpGet("docentes-con-cursos")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<DocenteConCursosDto>>> GetDocentesConCursos()
        {
            try
            {
                var docentes = await _horarioService.ObtenerDocentesConCursosActivosAsync();
                return Ok(docentes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener docentes con cursos", details = ex.Message });
            }
        }

        /// <summary>
        /// Crea múltiples horarios de una vez para un docente
        /// </summary>
        [HttpPost("batch")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ResultadoBatchHorariosDto>> CrearHorariosBatch([FromBody] CrearHorariosBatchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var resultado = await _horarioService.CrearHorariosBatchAsync(dto);
                
                if (resultado.TotalCreados == 0 && resultado.TotalFallidos > 0)
                {
                    return BadRequest(new { 
                        message = "No se pudo crear ningún horario", 
                        resultado 
                    });
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear horarios en batch", details = ex.Message });
            }
        }
    }
}
