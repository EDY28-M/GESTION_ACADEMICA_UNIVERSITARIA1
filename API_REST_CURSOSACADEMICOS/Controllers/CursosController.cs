using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CursosController : ControllerBase
    {
        private readonly ICursosService _cursosService;

        public CursosController(ICursosService cursosService)
        {
            _cursosService = cursosService;
        }

        // GET: api/Cursos
        [HttpGet]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<CursoDto>>> GetCursos()
        {
            return Ok(await _cursosService.GetCursosAsync());
        }

        // GET: api/Cursos/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<CursoDto>> GetCurso(int id)
        {
            var curso = await _cursosService.GetCursoAsync(id);

            if (curso == null)
            {
                return NotFound($"Curso con ID {id} no encontrado");
            }

            return Ok(curso);
        }

        // GET: api/Cursos/PorDocente/5
        [HttpGet("PorDocente/{docenteId}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<IEnumerable<CursoDto>>> GetCursosPorDocente(int docenteId)
        {
            var (exists, cursos) = await _cursosService.GetCursosPorDocenteAsync(docenteId);
            if (!exists) return NotFound($"Docente con ID {docenteId} no encontrado");
            return Ok(cursos);
        }

        // GET: api/Cursos/PorCiclo/1
        [HttpGet("PorCiclo/{ciclo}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<CursoDto>>> GetCursosPorCiclo(int ciclo)
        {
            return Ok(await _cursosService.GetCursosPorCicloAsync(ciclo));
        }

        // POST: api/Cursos
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<CursoDto>> PostCurso(CursoCreateDto cursoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, error, created) = await _cursosService.CreateCursoAsync(cursoDto);
            if (!success) return BadRequest(error);
            return CreatedAtAction(nameof(GetCurso), new { id = created!.Id }, created);
        }

        // PUT: api/Cursos/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutCurso(int id, CursoUpdateDto cursoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (notFound, success, error) = await _cursosService.UpdateCursoAsync(id, cursoDto);
            if (notFound) return NotFound($"Curso con ID {id} no encontrado");
            if (!success) return BadRequest(error);
            return NoContent();
        }

        // DELETE: api/Cursos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteCurso(int id)
        {
            var deleted = await _cursosService.DeleteCursoAsync(id);
            if (!deleted) return NotFound($"Curso con ID {id} no encontrado");
            return NoContent();
        }
    }
}
