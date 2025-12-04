using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;

namespace API_REST_CURSOSACADEMICOS.Services
{
    public class HorarioService : IHorarioService
    {
        private readonly GestionAcademicaContext _context;

        public HorarioService(GestionAcademicaContext context)
        {
            _context = context;
        }

        public async Task<HorarioDto> CrearHorarioAsync(CrearHorarioDto horarioDto)
        {
            // Validar que HoraInicio < HoraFin
            var inicio = TimeSpan.Parse(horarioDto.HoraInicio);
            var fin = TimeSpan.Parse(horarioDto.HoraFin);

            if (inicio >= fin)
            {
                throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");
            }

            // Validar cruces
            var conflicto = await ValidarCruceHorarioAsync(horarioDto);
            if (conflicto.HayConflicto)
            {
                throw new InvalidOperationException(conflicto.Mensaje);
            }

            var horario = new Horario
            {
                IdCurso = horarioDto.IdCurso,
                DiaSemana = horarioDto.DiaSemana,
                HoraInicio = inicio,
                HoraFin = fin,
                Aula = horarioDto.Aula,
                Tipo = horarioDto.Tipo
            };

            _context.Horarios.Add(horario);
            await _context.SaveChangesAsync();

            // Cargar datos relacionados para el DTO de respuesta
            var curso = await _context.Cursos
                .Include(c => c.Docente)
                .FirstOrDefaultAsync(c => c.Id == horario.IdCurso);

            if (curso == null) throw new InvalidOperationException("Error al recuperar el curso recién asignado.");

            return MapToDto(horario, curso);
        }

        public async Task<bool> EliminarHorarioAsync(int id)
        {
            var horario = await _context.Horarios.FindAsync(id);
            if (horario == null) return false;

            _context.Horarios.Remove(horario);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<HorarioDto>> ObtenerPorCursoAsync(int idCurso)
        {
            var horarios = await _context.Horarios
                .Include(h => h.Curso)
                .ThenInclude(c => c.Docente)
                .Where(h => h.IdCurso == idCurso)
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            return horarios.Select(h => MapToDto(h, h.Curso));
        }

        public async Task<IEnumerable<HorarioDto>> ObtenerPorDocenteAsync(int idDocente)
        {
            // Obtener el período activo
            var periodoActivo = await _context.Periodos
                .Where(p => p.Activo == true)
                .FirstOrDefaultAsync();

            if (periodoActivo == null)
            {
                // Si no hay período activo, retornar lista vacía
                return Enumerable.Empty<HorarioDto>();
            }

            // Obtener cursos que tienen matrículas en el período activo
            var cursosConMatriculasActivas = await _context.Matriculas
                .Where(m => m.IdPeriodo == periodoActivo.Id)
                .Select(m => m.IdCurso)
                .Distinct()
                .ToListAsync();

            var horarios = await _context.Horarios
                .Include(h => h.Curso)
                .ThenInclude(c => c.Docente)
                .Where(h => h.Curso.IdDocente == idDocente && cursosConMatriculasActivas.Contains(h.IdCurso))
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            return horarios.Select(h => MapToDto(h, h.Curso));
        }

        public async Task<IEnumerable<HorarioDto>> ObtenerPorEstudianteAsync(int idEstudiante)
        {
            // Obtener el período activo
            var periodoActivo = await _context.Periodos
                .Where(p => p.Activo == true)
                .FirstOrDefaultAsync();

            if (periodoActivo == null)
            {
                // Si no hay período activo, retornar lista vacía
                return Enumerable.Empty<HorarioDto>();
            }

            // Obtener cursos matriculados activos DEL PERÍODO ACTIVO
            var cursosMatriculados = await _context.Matriculas
                .Where(m => m.IdEstudiante == idEstudiante 
                         && m.Estado == "Matriculado"
                         && m.IdPeriodo == periodoActivo.Id)
                .Select(m => m.IdCurso)
                .ToListAsync();

            var horarios = await _context.Horarios
                .Include(h => h.Curso)
                .ThenInclude(c => c.Docente)
                .Where(h => cursosMatriculados.Contains(h.IdCurso))
                .OrderBy(h => h.DiaSemana)
                .ThenBy(h => h.HoraInicio)
                .ToListAsync();

            return horarios.Select(h => MapToDto(h, h.Curso));
        }

        public async Task<HorarioConflictoDto> ValidarCruceHorarioAsync(CrearHorarioDto horarioDto, int? idHorarioExcluir = null)
        {
            var curso = await _context.Cursos.FindAsync(horarioDto.IdCurso);
            if (curso == null)
            {
                throw new ArgumentException("El curso especificado no existe.");
            }

            if (curso.IdDocente == null)
            {
                // Si no hay docente asignado, no validamos cruce de docente, pero podríamos validar cruce de aula si se quisiera.
                // Por ahora solo validamos cruce de docente según requerimiento.
                return new HorarioConflictoDto { HayConflicto = false };
            }

            var inicio = TimeSpan.Parse(horarioDto.HoraInicio);
            var fin = TimeSpan.Parse(horarioDto.HoraFin);

            // Buscar otros horarios del mismo docente en el mismo día
            var horariosDocente = await _context.Horarios
                .Include(h => h.Curso)
                .Where(h => h.Curso.IdDocente == curso.IdDocente && 
                            h.DiaSemana == horarioDto.DiaSemana &&
                            h.Id != idHorarioExcluir) // Excluir el mismo horario si estamos editando
                .ToListAsync();

            foreach (var h in horariosDocente)
            {
                // Verificar superposición: (StartA < EndB) && (EndA > StartB)
                if (inicio < h.HoraFin && fin > h.HoraInicio)
                {
                    return new HorarioConflictoDto
                    {
                        HayConflicto = true,
                        Mensaje = $"Conflicto de horario con el curso {h.Curso.NombreCurso}",
                        CursoConflicto = h.Curso.NombreCurso,
                        HorarioConflicto = $"{h.HoraInicio:hh\\:mm} - {h.HoraFin:hh\\:mm}"
                    };
                }
            }

            return new HorarioConflictoDto { HayConflicto = false };
        }

        private HorarioDto MapToDto(Horario horario, Curso curso)
        {
            string[] dias = { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
            string diaTexto = (horario.DiaSemana >= 1 && horario.DiaSemana <= 7) ? dias[horario.DiaSemana] : "Desconocido";

            return new HorarioDto
            {
                Id = horario.Id,
                IdCurso = horario.IdCurso,
                NombreCurso = curso?.NombreCurso ?? "Desconocido",
                NombreDocente = curso?.Docente != null ? $"{curso.Docente.Nombres} {curso.Docente.Apellidos}" : "Sin asignar",
                DiaSemana = horario.DiaSemana,
                DiaSemanaTexto = diaTexto,
                HoraInicio = horario.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = horario.HoraFin.ToString(@"hh\:mm"),
                Aula = horario.Aula,
                Tipo = horario.Tipo
            };
        }
    }
}
