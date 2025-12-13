using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public class CursosService : ICursosService
{
    private readonly GestionAcademicaContext _context;

    public CursosService(GestionAcademicaContext context)
    {
        _context = context;
    }

    public async Task<List<CursoDto>> GetCursosAsync()
    {
        return await _context.Cursos
            .Include(c => c.Docente)
            .Include(c => c.PrerequisitosRequeridos)
                .ThenInclude(cp => cp.Prerequisito)
            .Select(MapCursoFullDtoExpression())
            .ToListAsync();
    }

    public async Task<CursoDto?> GetCursoAsync(int id)
    {
        return await _context.Cursos
            .Include(c => c.Docente)
            .Include(c => c.PrerequisitosRequeridos)
                .ThenInclude(cp => cp.Prerequisito)
            .Where(c => c.Id == id)
            .Select(MapCursoFullDtoExpression())
            .FirstOrDefaultAsync();
    }

    public async Task<(bool exists, List<CursoDto> cursos)> GetCursosPorDocenteAsync(int docenteId)
    {
        var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == docenteId);
        if (!docenteExiste)
        {
            return (false, new List<CursoDto>());
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

        return (true, cursos);
    }

    public async Task<List<CursoDto>> GetCursosPorCicloAsync(int ciclo)
    {
        return await _context.Cursos
            .Include(c => c.Docente)
            .Where(c => c.Ciclo == ciclo)
            .Select(c => new CursoDto
            {
                Id = c.Id,
                NombreCurso = c.NombreCurso,
                Creditos = c.Creditos,
                HorasSemanal = c.HorasSemanal,
                Ciclo = c.Ciclo,
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
    }

    public async Task<(bool success, string? error, CursoDto? created)> CreateCursoAsync(CursoCreateDto dto)
    {
        // Verificar si el docente existe
        if (dto.IdDocente.HasValue)
        {
            var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == dto.IdDocente);
            if (!docenteExiste)
            {
                return (false, $"Docente con ID {dto.IdDocente} no encontrado", null);
            }
        }

        // Verificar que los prerequisitos existan
        if (dto.PrerequisitosIds.Any())
        {
            foreach (var prereqId in dto.PrerequisitosIds)
            {
                var cursoExiste = await _context.Cursos.AnyAsync(c => c.Id == prereqId);
                if (!cursoExiste)
                {
                    return (false, $"Curso prerequisito con ID {prereqId} no encontrado", null);
                }
            }
        }

        var curso = new Curso
        {
            Codigo = dto.Codigo,
            NombreCurso = dto.NombreCurso,
            Creditos = dto.Creditos,
            HorasSemanal = dto.HorasSemanal,
            HorasTeoria = dto.HorasTeoria,
            HorasPractica = dto.HorasPractica,
            HorasTotales = dto.HorasTotales,
            Ciclo = dto.Ciclo,
            IdDocente = dto.IdDocente
        };

        _context.Cursos.Add(curso);
        await _context.SaveChangesAsync();

        // Agregar prerequisitos
        if (dto.PrerequisitosIds.Any())
        {
            foreach (var prereqId in dto.PrerequisitosIds)
            {
                _context.CursoPrerequisitos.Add(new CursoPrerequisito
                {
                    IdCurso = curso.Id,
                    IdCursoPrerequisito = prereqId
                });
            }
            await _context.SaveChangesAsync();
        }

        // Cargar el docente y prerequisitos para la respuesta
        await _context.Entry(curso).Reference(c => c.Docente).LoadAsync();
        await _context.Entry(curso).Collection(c => c.PrerequisitosRequeridos).LoadAsync();

        foreach (var prereq in curso.PrerequisitosRequeridos)
        {
            await _context.Entry(prereq).Reference(cp => cp.Prerequisito).LoadAsync();
        }

        var response = new CursoDto
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

        return (true, null, response);
    }

    public async Task<(bool notFound, bool success, string? error)> UpdateCursoAsync(int id, CursoUpdateDto dto)
    {
        var curso = await _context.Cursos
            .Include(c => c.PrerequisitosRequeridos)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso == null)
        {
            return (true, false, $"Curso con ID {id} no encontrado");
        }

        // Verificar si el docente existe
        if (dto.IdDocente.HasValue)
        {
            var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == dto.IdDocente);
            if (!docenteExiste)
            {
                return (false, false, $"Docente con ID {dto.IdDocente} no encontrado");
            }
        }

        // Verificar que los prerequisitos existan
        if (dto.PrerequisitosIds.Any())
        {
            foreach (var prereqId in dto.PrerequisitosIds)
            {
                var cursoExiste = await _context.Cursos.AnyAsync(c => c.Id == prereqId);
                if (!cursoExiste)
                {
                    return (false, false, $"Curso prerequisito con ID {prereqId} no encontrado");
                }
            }
        }

        curso.Codigo = dto.Codigo;
        curso.NombreCurso = dto.NombreCurso;
        curso.Creditos = dto.Creditos;
        curso.HorasSemanal = dto.HorasSemanal;
        curso.HorasTeoria = dto.HorasTeoria;
        curso.HorasPractica = dto.HorasPractica;
        curso.HorasTotales = dto.HorasTotales;
        curso.Ciclo = dto.Ciclo;
        curso.IdDocente = dto.IdDocente;

        // Actualizar prerequisitos
        _context.CursoPrerequisitos.RemoveRange(curso.PrerequisitosRequeridos);

        if (dto.PrerequisitosIds.Any())
        {
            foreach (var prereqId in dto.PrerequisitosIds)
            {
                _context.CursoPrerequisitos.Add(new CursoPrerequisito
                {
                    IdCurso = curso.Id,
                    IdCursoPrerequisito = prereqId
                });
            }
        }

        await _context.SaveChangesAsync();
        return (false, true, null);
    }

    public async Task<bool> DeleteCursoAsync(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            return false;
        }

        _context.Cursos.Remove(curso);
        await _context.SaveChangesAsync();
        return true;
    }

    private static System.Linq.Expressions.Expression<Func<Curso, CursoDto>> MapCursoFullDtoExpression()
    {
        return c => new CursoDto
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
        };
    }
}


