using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Data;
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
        private readonly GestionAcademicaContext _context;

        public HorariosController(IHorarioService horarioService, GestionAcademicaContext context)
        {
            _horarioService = horarioService;
            _context = context;
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

                var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Correo == email);
                if (docente == null)
                {
                    return NotFound(new { message = "Perfil de docente no encontrado para este usuario." });
                }

                var horarios = await _horarioService.ObtenerPorDocenteAsync(docente.Id);
                return Ok(horarios);
            }
            else if (role == "Estudiante")
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.IdUsuario == userId);
                if (estudiante == null)
                {
                    // Intentar buscar por email si IdUsuario no está seteado (caso legacy o error)
                    if (!string.IsNullOrEmpty(email))
                    {
                        estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.Correo == email);
                    }
                }

                if (estudiante == null)
                {
                    return NotFound(new { message = "Perfil de estudiante no encontrado para este usuario." });
                }

                var horarios = await _horarioService.ObtenerPorEstudianteAsync(estudiante.Id);
                return Ok(horarios);
            }

            return BadRequest(new { message = "Rol no válido para esta operación." });
        }
    }
}
