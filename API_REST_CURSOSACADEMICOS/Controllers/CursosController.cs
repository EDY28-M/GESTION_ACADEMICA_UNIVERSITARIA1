using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CursosController : ControllerBase
    {
        private readonly GestionAcademicaContext _context;

        public CursosController(GestionAcademicaContext context)
        {
            _context = context;
        }

        // GET: api/Cursos
        [HttpGet]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<CursoDto>>> GetCursos()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Docente)
                .Include(c => c.PrerequisitosRequeridos)
                    .ThenInclude(cp => cp.Prerequisito)
                .Select(c => new CursoDto
                {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    HorasTeoria = c.HorasTeoria,
                    HorasPractica = c.HorasPractica,
                    HorasTotales = c.HorasTotales,
                    Ciclo = c.Ciclo,
                    Semestre = c.Semestre,
                    IdDocente = c.IdDocente,
                    Docente = c.Docente != null ? new DocenteSimpleDto
                    {
                        Id = c.Docente.Id,
                        Apellidos = c.Docente.Apellidos,
                        Nombres = c.Docente.Nombres,
                        Profesion = c.Docente.Profesion
                    } : null,
                    PrerequisitosIds = c.PrerequisitosRequeridos.Select(p => p.IdCursoPrerequisito).ToList(),
                    Prerequisitos = c.PrerequisitosRequeridos.Select(p => new CursoSimpleDto
                    {
                        Id = p.Prerequisito.Id,
                        Codigo = p.Prerequisito.Codigo,
                        NombreCurso = p.Prerequisito.NombreCurso,
                        Ciclo = p.Prerequisito.Ciclo
                    }).ToList()
                })
                .ToListAsync();

            return Ok(cursos);
        }

        // GET: api/Cursos/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<CursoDto>> GetCurso(int id)
        {
            var curso = await _context.Cursos
                .Include(c => c.Docente)
                .Include(c => c.PrerequisitosRequeridos)
                    .ThenInclude(cp => cp.Prerequisito)
                .Where(c => c.Id == id)
                .Select(c => new CursoDto
                {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    HorasTeoria = c.HorasTeoria,
                    HorasPractica = c.HorasPractica,
                    HorasTotales = c.HorasTotales,
                    Ciclo = c.Ciclo,
                    Semestre = c.Semestre,
                    IdDocente = c.IdDocente,
                    Docente = c.Docente != null ? new DocenteSimpleDto
                    {
                        Id = c.Docente.Id,
                        Apellidos = c.Docente.Apellidos,
                        Nombres = c.Docente.Nombres,
                        Profesion = c.Docente.Profesion
                    } : null,
                    PrerequisitosIds = c.PrerequisitosRequeridos.Select(p => p.IdCursoPrerequisito).ToList(),
                    Prerequisitos = c.PrerequisitosRequeridos.Select(p => new CursoSimpleDto
                    {
                        Id = p.Prerequisito.Id,
                        Codigo = p.Prerequisito.Codigo,
                        NombreCurso = p.Prerequisito.NombreCurso,
                        Ciclo = p.Prerequisito.Ciclo
                    }).ToList()
                })
                .FirstOrDefaultAsync();

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
            var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == docenteId);
            if (!docenteExiste)
            {
                return NotFound($"Docente con ID {docenteId} no encontrado");
            }

            var cursos = await _context.Cursos
                .Include(c => c.Docente)
                .Where(c => c.IdDocente == docenteId)
                .Select(c => new CursoDto
                {
                    Id = c.Id,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    Ciclo = c.Ciclo,
                    Semestre = c.Semestre,
                    IdDocente = c.IdDocente,
                    Docente = c.Docente != null ? new DocenteSimpleDto
                    {
                        Id = c.Docente.Id,
                        Apellidos = c.Docente.Apellidos,
                        Nombres = c.Docente.Nombres,
                        Profesion = c.Docente.Profesion
                    } : null
                })
                .ToListAsync();

            return Ok(cursos);
        }

        // GET: api/Cursos/PorCiclo/1
        [HttpGet("PorCiclo/{ciclo}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante")]
        public async Task<ActionResult<IEnumerable<CursoDto>>> GetCursosPorCiclo(int ciclo)
        {
            var cursos = await _context.Cursos
                .Include(c => c.Docente)
                .Where(c => c.Ciclo == ciclo)
                .Select(c => new CursoDto
                {
                    Id = c.Id,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    Ciclo = c.Ciclo,
                    Semestre = c.Semestre,
                    IdDocente = c.IdDocente,
                    Docente = c.Docente != null ? new DocenteSimpleDto
                    {
                        Id = c.Docente.Id,
                        Apellidos = c.Docente.Apellidos,
                        Nombres = c.Docente.Nombres,
                        Profesion = c.Docente.Profesion
                    } : null
                })
                .ToListAsync();

            return Ok(cursos);
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

            // Verificar si el docente existe
            if (cursoDto.IdDocente.HasValue)
            {
                var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == cursoDto.IdDocente);
                if (!docenteExiste)
                {
                    return BadRequest($"Docente con ID {cursoDto.IdDocente} no encontrado");
                }
            }

            // Verificar que los prerequisitos existan
            if (cursoDto.PrerequisitosIds.Any())
            {
                foreach (var prereqId in cursoDto.PrerequisitosIds)
                {
                    var cursoExiste = await _context.Cursos.AnyAsync(c => c.Id == prereqId);
                    if (!cursoExiste)
                    {
                        return BadRequest($"Curso prerequisito con ID {prereqId} no encontrado");
                    }
                }
            }

            var curso = new Curso
            {
                Codigo = cursoDto.Codigo,
                NombreCurso = cursoDto.NombreCurso,
                Creditos = cursoDto.Creditos,
                HorasSemanal = cursoDto.HorasSemanal,
                HorasTeoria = cursoDto.HorasTeoria,
                HorasPractica = cursoDto.HorasPractica,
                HorasTotales = cursoDto.HorasTotales,
                Ciclo = cursoDto.Ciclo,
                Semestre = cursoDto.Semestre,
                IdDocente = cursoDto.IdDocente
            };

            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            // Agregar prerequisitos
            if (cursoDto.PrerequisitosIds.Any())
            {
                foreach (var prereqId in cursoDto.PrerequisitosIds)
                {
                    var cursoPrerequisito = new CursoPrerequisito
                    {
                        IdCurso = curso.Id,
                        IdCursoPrerequisito = prereqId
                    };
                    _context.CursoPrerequisitos.Add(cursoPrerequisito);
                }
                await _context.SaveChangesAsync();
            }

            // Cargar el docente y prerequisitos para la respuesta
            await _context.Entry(curso)
                .Reference(c => c.Docente)
                .LoadAsync();

            await _context.Entry(curso)
                .Collection(c => c.PrerequisitosRequeridos)
                .LoadAsync();

            foreach (var prereq in curso.PrerequisitosRequeridos)
            {
                await _context.Entry(prereq)
                    .Reference(cp => cp.Prerequisito)
                    .LoadAsync();
            }

            var cursoResponse = new CursoDto
            {
                Id = curso.Id,
                Codigo = curso.Codigo,
                NombreCurso = curso.NombreCurso,
                Creditos = curso.Creditos,
                HorasSemanal = curso.HorasSemanal,
                HorasTeoria = curso.HorasTeoria,
                HorasPractica = curso.HorasPractica,
                HorasTotales = curso.HorasTotales,
                Ciclo = curso.Ciclo,
                Semestre = curso.Semestre,
                IdDocente = curso.IdDocente,
                Docente = curso.Docente != null ? new DocenteSimpleDto
                {
                    Id = curso.Docente.Id,
                    Apellidos = curso.Docente.Apellidos,
                    Nombres = curso.Docente.Nombres,
                    Profesion = curso.Docente.Profesion
                } : null,
                PrerequisitosIds = curso.PrerequisitosRequeridos.Select(p => p.IdCursoPrerequisito).ToList(),
                Prerequisitos = curso.PrerequisitosRequeridos.Select(p => new CursoSimpleDto
                {
                    Id = p.Prerequisito.Id,
                    Codigo = p.Prerequisito.Codigo,
                    NombreCurso = p.Prerequisito.NombreCurso,
                    Ciclo = p.Prerequisito.Ciclo
                }).ToList()
            };

            return CreatedAtAction(nameof(GetCurso), new { id = curso.Id }, cursoResponse);
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

            var curso = await _context.Cursos
                .Include(c => c.PrerequisitosRequeridos)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (curso == null)
            {
                return NotFound($"Curso con ID {id} no encontrado");
            }

            // Verificar si el docente existe
            if (cursoDto.IdDocente.HasValue)
            {
                var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == cursoDto.IdDocente);
                if (!docenteExiste)
                {
                    return BadRequest($"Docente con ID {cursoDto.IdDocente} no encontrado");
                }
            }

            // Verificar que los prerequisitos existan
            if (cursoDto.PrerequisitosIds.Any())
            {
                foreach (var prereqId in cursoDto.PrerequisitosIds)
                {
                    var cursoExiste = await _context.Cursos.AnyAsync(c => c.Id == prereqId);
                    if (!cursoExiste)
                    {
                        return BadRequest($"Curso prerequisito con ID {prereqId} no encontrado");
                    }
                }
            }

            curso.Codigo = cursoDto.Codigo;
            curso.NombreCurso = cursoDto.NombreCurso;
            curso.Creditos = cursoDto.Creditos;
            curso.HorasSemanal = cursoDto.HorasSemanal;
            curso.HorasTeoria = cursoDto.HorasTeoria;
            curso.HorasPractica = cursoDto.HorasPractica;
            curso.HorasTotales = cursoDto.HorasTotales;
            curso.Ciclo = cursoDto.Ciclo;
            curso.Semestre = cursoDto.Semestre;
            curso.IdDocente = cursoDto.IdDocente;

            // Actualizar prerequisitos
            // Eliminar los anteriores
            _context.CursoPrerequisitos.RemoveRange(curso.PrerequisitosRequeridos);

            // Agregar los nuevos
            if (cursoDto.PrerequisitosIds.Any())
            {
                foreach (var prereqId in cursoDto.PrerequisitosIds)
                {
                    var cursoPrerequisito = new CursoPrerequisito
                    {
                        IdCurso = curso.Id,
                        IdCursoPrerequisito = prereqId
                    };
                    _context.CursoPrerequisitos.Add(cursoPrerequisito);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CursoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Cursos/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteCurso(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                return NotFound($"Curso con ID {id} no encontrado");
            }

            _context.Cursos.Remove(curso);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CursoExists(int id)
        {
            return _context.Cursos.Any(e => e.Id == id);
        }
    }
}
