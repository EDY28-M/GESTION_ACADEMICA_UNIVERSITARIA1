using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace API_REST_CURSOSACADEMICOS.Services
{
    /// <summary>
    /// Servicio de gestión de asistencias con lógica de negocio completa
    /// Implementa patrones Repository y Service siguiendo principios SOLID
    /// </summary>
    public class AsistenciaService : IAsistenciaService
    {
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<AsistenciaService> _logger;

        public AsistenciaService(GestionAcademicaContext context, ILogger<AsistenciaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================
        // IMPLEMENTACIÓN MÉTODOS PARA DOCENTES
        // ============================================

        public async Task<AsistenciaDto> RegistrarAsistenciaAsync(RegistrarAsistenciaDto dto)
        {
            try
            {
                // Validar que no exista ya una asistencia para ese estudiante, curso, fecha y tipo de clase
                var existente = await _context.Asistencias
                    .FirstOrDefaultAsync(a =>
                        a.IdEstudiante == dto.IdEstudiante &&
                        a.IdCurso == dto.IdCurso &&
                        a.Fecha.Date == dto.Fecha.Date &&
                        a.TipoClase == dto.TipoClase);

                if (existente != null)
                {
                    throw new InvalidOperationException(
                        $"Ya existe un registro de asistencia de {dto.TipoClase} para este estudiante en esta fecha");
                }

                // Validar que el estudiante exista
                var estudiante = await _context.Estudiantes
                    .Include(e => e.Usuario)
                    .FirstOrDefaultAsync(e => e.Id == dto.IdEstudiante);

                if (estudiante == null)
                {
                    throw new ArgumentException($"Estudiante con ID {dto.IdEstudiante} no encontrado");
                }

                // Validar que el curso exista
                var curso = await _context.Cursos.FindAsync(dto.IdCurso);
                if (curso == null)
                {
                    throw new ArgumentException($"Curso con ID {dto.IdCurso} no encontrado");
                }

                // Crear nueva asistencia
                var asistencia = new Asistencia
                {
                    IdEstudiante = dto.IdEstudiante,
                    IdCurso = dto.IdCurso,
                    Fecha = dto.Fecha.Date, // Solo guardar la fecha sin hora
                    Presente = dto.Presente,
                    TipoClase = dto.TipoClase,
                    Observaciones = dto.Observaciones,
                    FechaRegistro = DateTime.Now
                };

                _context.Asistencias.Add(asistencia);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Asistencia registrada: Estudiante {IdEstudiante}, Curso {IdCurso}, Fecha {Fecha}, Tipo: {TipoClase}, Presente: {Presente}",
                    dto.IdEstudiante, dto.IdCurso, dto.Fecha.ToShortDateString(), dto.TipoClase, dto.Presente);

                // Retornar DTO con información completa
                return new AsistenciaDto
                {
                    Id = asistencia.Id,
                    IdEstudiante = asistencia.IdEstudiante,
                    NombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                    IdCurso = asistencia.IdCurso,
                    NombreCurso = curso.NombreCurso,
                    Fecha = asistencia.Fecha,
                    Presente = asistencia.Presente,
                    TipoClase = asistencia.TipoClase,
                    Observaciones = asistencia.Observaciones,
                    FechaRegistro = asistencia.FechaRegistro
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencia");
                throw;
            }
        }

        public async Task<List<AsistenciaDto>> RegistrarAsistenciasMasivasAsync(RegistrarAsistenciasMasivasDto dto)
        {
            try
            {
                if (dto.Asistencias == null || !dto.Asistencias.Any())
                {
                    throw new ArgumentException("La lista de asistencias no puede estar vacía");
                }

                // Validar que el curso exista
                var curso = await _context.Cursos.FindAsync(dto.IdCurso);
                if (curso == null)
                {
                    throw new ArgumentException($"Curso con ID {dto.IdCurso} no encontrado");
                }

                var resultados = new List<AsistenciaDto>();
                var fecha = dto.Fecha.Date;

                // Obtener asistencias existentes para esta fecha, curso y tipo de clase
                var asistenciasExistentes = await _context.Asistencias
                    .Where(a => a.IdCurso == dto.IdCurso && a.Fecha.Date == fecha && a.TipoClase == dto.TipoClase)
                    .ToListAsync();

                // Obtener información de estudiantes
                var idsEstudiantes = dto.Asistencias.Select(a => a.IdEstudiante).ToList();
                var estudiantes = await _context.Estudiantes
                    .Where(e => idsEstudiantes.Contains(e.Id))
                    .ToListAsync();

                foreach (var asistenciaDto in dto.Asistencias)
                {
                    var estudiante = estudiantes.FirstOrDefault(e => e.Id == asistenciaDto.IdEstudiante);
                    if (estudiante == null)
                    {
                        _logger.LogWarning("Estudiante {IdEstudiante} no encontrado, omitiendo", asistenciaDto.IdEstudiante);
                        continue;
                    }

                    // Verificar si ya existe asistencia para este estudiante en este tipo de clase
                    var existente = asistenciasExistentes
                        .FirstOrDefault(a => a.IdEstudiante == asistenciaDto.IdEstudiante);

                    Asistencia asistencia;

                    if (existente != null)
                    {
                        // Actualizar existente
                        existente.Presente = asistenciaDto.Presente;
                        existente.Observaciones = asistenciaDto.Observaciones;
                        asistencia = existente;
                    }
                    else
                    {
                        // Crear nueva
                        asistencia = new Asistencia
                        {
                            IdEstudiante = asistenciaDto.IdEstudiante,
                            IdCurso = dto.IdCurso,
                            Fecha = fecha,
                            Presente = asistenciaDto.Presente,
                            TipoClase = dto.TipoClase,
                            Observaciones = asistenciaDto.Observaciones,
                            FechaRegistro = DateTime.Now
                        };
                        _context.Asistencias.Add(asistencia);
                    }

                    resultados.Add(new AsistenciaDto
                    {
                        Id = asistencia.Id,
                        IdEstudiante = asistencia.IdEstudiante,
                        NombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                        IdCurso = asistencia.IdCurso,
                        NombreCurso = curso.NombreCurso,
                        Fecha = asistencia.Fecha,
                        Presente = asistencia.Presente,
                        TipoClase = asistencia.TipoClase,
                        Observaciones = asistencia.Observaciones,
                        FechaRegistro = asistencia.FechaRegistro
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Asistencias masivas registradas: Curso {IdCurso}, Fecha {Fecha}, Total: {Total}",
                    dto.IdCurso, fecha.ToShortDateString(), resultados.Count);

                return resultados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencias masivas");
                throw;
            }
        }

        public async Task<AsistenciaDto> ActualizarAsistenciaAsync(int idAsistencia, ActualizarAsistenciaDto dto)
        {
            try
            {
                var asistencia = await _context.Asistencias
                    .Include(a => a.Estudiante)
                    .Include(a => a.Curso)
                    .FirstOrDefaultAsync(a => a.Id == idAsistencia);

                if (asistencia == null)
                {
                    throw new ArgumentException($"Asistencia con ID {idAsistencia} no encontrada");
                }

                if (dto.Fecha.HasValue)
                {
                    asistencia.Fecha = dto.Fecha.Value;
                }
                asistencia.Presente = dto.Presente;
                if (!string.IsNullOrEmpty(dto.TipoClase))
                {
                    asistencia.TipoClase = dto.TipoClase;
                }
                asistencia.Observaciones = dto.Observaciones;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Asistencia actualizada: ID {IdAsistencia}", idAsistencia);

                return new AsistenciaDto
                {
                    Id = asistencia.Id,
                    IdEstudiante = asistencia.IdEstudiante,
                    NombreEstudiante = $"{asistencia.Estudiante?.Nombres} {asistencia.Estudiante?.Apellidos}",
                    IdCurso = asistencia.IdCurso,
                    NombreCurso = asistencia.Curso?.NombreCurso ?? "",
                    Fecha = asistencia.Fecha,
                    Presente = asistencia.Presente,
                    TipoClase = asistencia.TipoClase,
                    Observaciones = asistencia.Observaciones,
                    FechaRegistro = asistencia.FechaRegistro
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar asistencia");
                throw;
            }
        }

        public async Task<bool> EliminarAsistenciaAsync(int idAsistencia)
        {
            try
            {
                var asistencia = await _context.Asistencias.FindAsync(idAsistencia);
                if (asistencia == null)
                {
                    return false;
                }

                _context.Asistencias.Remove(asistencia);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Asistencia eliminada: ID {IdAsistencia}", idAsistencia);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar asistencia");
                throw;
            }
        }

        public async Task<ResumenAsistenciaCursoDto> GetResumenAsistenciaCursoAsync(
            int idCurso, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                var curso = await _context.Cursos
                    .Include(c => c.Docente)
                    .FirstOrDefaultAsync(c => c.Id == idCurso);

                if (curso == null)
                {
                    throw new ArgumentException($"Curso con ID {idCurso} no encontrado");
                }

                // Obtener período activo si no se especifican fechas
                if (!fechaInicio.HasValue || !fechaFin.HasValue)
                {
                    var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
                    if (periodoActivo != null)
                    {
                        fechaInicio ??= periodoActivo.FechaInicio;
                        fechaFin ??= periodoActivo.FechaFin;
                    }
                }

                // Obtener estudiantes matriculados en el curso
                var estudiantes = await _context.Matriculas
                    .Where(m => m.IdCurso == idCurso && m.Estado == "Matriculado")
                    .Include(m => m.Estudiante)
                    .Select(m => m.Estudiante!)
                    .ToListAsync();

                // Obtener asistencias del curso en el rango de fechas
                var query = _context.Asistencias.Where(a => a.IdCurso == idCurso);

                if (fechaInicio.HasValue)
                    query = query.Where(a => a.Fecha >= fechaInicio.Value);
                if (fechaFin.HasValue)
                    query = query.Where(a => a.Fecha <= fechaFin.Value);

                var asistencias = await query.ToListAsync();

                // Obtener fechas únicas de clases
                var fechasClases = asistencias.Select(a => a.Fecha.Date).Distinct().OrderBy(f => f).ToList();
                var totalClases = fechasClases.Count;

                // Calcular resumen por estudiante
                var resumenEstudiantes = new List<ResumenAsistenciaEstudianteSimpleDto>();

                foreach (var estudiante in estudiantes)
                {
                    var asistenciasEstudiante = asistencias.Where(a => a.IdEstudiante == estudiante.Id).ToList();
                    var totalAsistencias = asistenciasEstudiante.Count(a => a.Presente);
                    var totalFaltas = asistenciasEstudiante.Count(a => !a.Presente);
                    var porcentaje = totalClases > 0 ? (decimal)totalAsistencias / totalClases * 100 : 0;

                    resumenEstudiantes.Add(new ResumenAsistenciaEstudianteSimpleDto
                    {
                        IdEstudiante = estudiante.Id,
                        NombreCompleto = $"{estudiante.Nombres} {estudiante.Apellidos}",
                        Codigo = estudiante.Codigo,
                        TotalClases = totalClases,
                        TotalAsistencias = totalAsistencias,
                        TotalFaltas = totalFaltas,
                        PorcentajeAsistencia = Math.Round(porcentaje, 2)
                    });
                }

                var porcentajePromedio = resumenEstudiantes.Any()
                    ? resumenEstudiantes.Average(r => r.PorcentajeAsistencia)
                    : 0;

                return new ResumenAsistenciaCursoDto
                {
                    IdCurso = idCurso,
                    NombreCurso = curso.NombreCurso,
                    TotalEstudiantes = estudiantes.Count,
                    TotalClases = totalClases,
                    PorcentajeAsistenciaPromedio = Math.Round(porcentajePromedio, 2),
                    Estudiantes = resumenEstudiantes.OrderBy(r => r.NombreCompleto).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencias del curso");
                throw;
            }
        }

        public async Task<HistorialAsistenciasDto> GetHistorialAsistenciasAsync(FiltrosAsistenciaDto filtros)
        {
            try
            {
                var query = _context.Asistencias
                    .Include(a => a.Estudiante)
                    .Include(a => a.Curso)
                    .AsQueryable();

                // Aplicar filtros
                if (filtros.IdEstudiante.HasValue)
                    query = query.Where(a => a.IdEstudiante == filtros.IdEstudiante.Value);

                if (filtros.IdCurso.HasValue)
                    query = query.Where(a => a.IdCurso == filtros.IdCurso.Value);

                if (filtros.FechaInicio.HasValue)
                    query = query.Where(a => a.Fecha >= filtros.FechaInicio.Value);

                if (filtros.FechaFin.HasValue)
                    query = query.Where(a => a.Fecha <= filtros.FechaFin.Value);

                if (filtros.Presente.HasValue)
                    query = query.Where(a => a.Presente == filtros.Presente.Value);

                var asistencias = await query.OrderByDescending(a => a.Fecha).ToListAsync();

                var asistenciasDto = asistencias.Select(a => new AsistenciaDto
                {
                    Id = a.Id,
                    IdEstudiante = a.IdEstudiante,
                    NombreEstudiante = $"{a.Estudiante?.Nombres} {a.Estudiante?.Apellidos}",
                    IdCurso = a.IdCurso,
                    NombreCurso = a.Curso?.NombreCurso ?? "",
                    Fecha = a.Fecha,
                    Presente = a.Presente,
                    Observaciones = a.Observaciones,
                    FechaRegistro = a.FechaRegistro
                }).ToList();

                var totalAsistencias = asistencias.Count(a => a.Presente);
                var totalFaltas = asistencias.Count(a => !a.Presente);
                var porcentaje = asistencias.Any()
                    ? (decimal)totalAsistencias / asistencias.Count * 100
                    : 0;

                return new HistorialAsistenciasDto
                {
                    Asistencias = asistenciasDto,
                    TotalRegistros = asistencias.Count,
                    TotalAsistencias = totalAsistencias,
                    TotalFaltas = totalFaltas,
                    PorcentajeAsistencia = Math.Round(porcentaje, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de asistencias");
                throw;
            }
        }

        public async Task<ReporteAsistenciaDto> GenerarReporteAsistenciaAsync(
            int idCurso, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                var resumen = await GetResumenAsistenciaCursoAsync(idCurso, fechaInicio, fechaFin);
                var curso = await _context.Cursos.Include(c => c.Docente).FirstOrDefaultAsync(c => c.Id == idCurso);

                if (curso == null)
                {
                    throw new ArgumentException($"Curso con ID {idCurso} no encontrado");
                }

                // Obtener asistencias del rango
                var query = _context.Asistencias.Where(a => a.IdCurso == idCurso);
                if (fechaInicio.HasValue)
                    query = query.Where(a => a.Fecha >= fechaInicio.Value);
                if (fechaFin.HasValue)
                    query = query.Where(a => a.Fecha <= fechaFin.Value);

                var asistencias = await query.ToListAsync();
                var fechasClases = asistencias.Select(a => a.Fecha.Date).Distinct().OrderBy(f => f).ToList();

                // Construir reporte por estudiante
                var reporteEstudiantes = new List<ReporteAsistenciaEstudianteDto>();
                foreach (var est in resumen.Estudiantes)
                {
                    var asistenciasEst = asistencias.Where(a => a.IdEstudiante == est.IdEstudiante).ToList();
                    var asistenciasPorFecha = fechasClases.ToDictionary(
                        fecha => fecha,
                        fecha =>
                        {
                            var asist = asistenciasEst.FirstOrDefault(a => a.Fecha.Date == fecha);
                            return asist?.Presente ?? false;
                        }
                    );

                    reporteEstudiantes.Add(new ReporteAsistenciaEstudianteDto
                    {
                        Codigo = est.Codigo,
                        NombreCompleto = est.NombreCompleto,
                        TotalAsistencias = est.TotalAsistencias,
                        TotalFaltas = est.TotalFaltas,
                        PorcentajeAsistencia = est.PorcentajeAsistencia,
                        AsistenciasPorFecha = asistenciasPorFecha
                    });
                }

                // Construir reporte por fecha
                var reporteFechas = new List<ReporteFechaClaseDto>();
                foreach (var fecha in fechasClases)
                {
                    var asistenciasFecha = asistencias.Where(a => a.Fecha.Date == fecha).ToList();
                    var presentes = asistenciasFecha.Count(a => a.Presente);
                    var ausentes = asistenciasFecha.Count(a => !a.Presente);
                    var porcentaje = asistenciasFecha.Any()
                        ? (decimal)presentes / asistenciasFecha.Count * 100
                        : 0;

                    reporteFechas.Add(new ReporteFechaClaseDto
                    {
                        Fecha = fecha,
                        TotalPresentes = presentes,
                        TotalAusentes = ausentes,
                        PorcentajeAsistencia = Math.Round(porcentaje, 2)
                    });
                }

                return new ReporteAsistenciaDto
                {
                    NombreCurso = curso.NombreCurso,
                    NombreDocente = curso.Docente != null
                        ? $"{curso.Docente.Nombres} {curso.Docente.Apellidos}"
                        : null,
                    FechaGeneracion = DateTime.Now,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    TotalEstudiantes = resumen.TotalEstudiantes,
                    TotalClases = resumen.TotalClases,
                    PorcentajeAsistenciaPromedio = resumen.PorcentajeAsistenciaPromedio,
                    Estudiantes = reporteEstudiantes,
                    Fechas = reporteFechas
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de asistencias");
                throw;
            }
        }

        public async Task<List<AsistenciaDto>> GetAsistenciasPorCursoYFechaAsync(int idCurso, DateTime fecha)
        {
            try
            {
                var asistencias = await _context.Asistencias
                    .Include(a => a.Estudiante)
                    .Include(a => a.Curso)
                    .Where(a => a.IdCurso == idCurso && a.Fecha.Date == fecha.Date)
                    .OrderBy(a => a.Estudiante!.Apellidos)
                    .ToListAsync();

                return asistencias.Select(a => new AsistenciaDto
                {
                    Id = a.Id,
                    IdEstudiante = a.IdEstudiante,
                    NombreEstudiante = $"{a.Estudiante?.Nombres} {a.Estudiante?.Apellidos}",
                    IdCurso = a.IdCurso,
                    NombreCurso = a.Curso?.NombreCurso ?? "",
                    Fecha = a.Fecha,
                    Presente = a.Presente,
                    Observaciones = a.Observaciones,
                    FechaRegistro = a.FechaRegistro
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias por curso y fecha");
                throw;
            }
        }

        // ============================================
        // IMPLEMENTACIÓN MÉTODOS PARA ESTUDIANTES
        // ============================================

        public async Task<List<AsistenciasPorCursoDto>> GetAsistenciasPorEstudianteAsync(int idEstudiante, int? idPeriodo = null)
        {
            try
            {
                // Obtener cursos matriculados del estudiante
                var queryMatriculas = _context.Matriculas
                    .Include(m => m.Curso)
                        .ThenInclude(c => c!.Docente)
                    .Where(m => m.IdEstudiante == idEstudiante && m.Estado == "Matriculado");

                if (idPeriodo.HasValue)
                {
                    queryMatriculas = queryMatriculas.Where(m => m.IdPeriodo == idPeriodo.Value);
                }
                else
                {
                    // Si no se especifica período, usar el activo
                    var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
                    if (periodoActivo != null)
                    {
                        queryMatriculas = queryMatriculas.Where(m => m.IdPeriodo == periodoActivo.Id);
                    }
                }

                var matriculas = await queryMatriculas.ToListAsync();
                var resultado = new List<AsistenciasPorCursoDto>();

                foreach (var matricula in matriculas)
                {
                    if (matricula.Curso == null) continue;

                    var asistencias = await _context.Asistencias
                        .Where(a => a.IdEstudiante == idEstudiante && a.IdCurso == matricula.IdCurso)
                        .OrderBy(a => a.Fecha)
                        .ToListAsync();

                    var totalClases = asistencias.Count;
                    var totalAsistencias = asistencias.Count(a => a.Presente);
                    var totalFaltas = asistencias.Count(a => !a.Presente);
                    var porcentaje = totalClases > 0 ? (decimal)totalAsistencias / totalClases * 100 : 0;

                    resultado.Add(new AsistenciasPorCursoDto
                    {
                        IdCurso = matricula.IdCurso,
                        CodigoCurso = matricula.Curso.Codigo ?? "",
                        NombreCurso = matricula.Curso.NombreCurso,
                        Creditos = matricula.Curso.Creditos,
                        NombreDocente = matricula.Curso.Docente != null
                            ? $"{matricula.Curso.Docente.Nombres} {matricula.Curso.Docente.Apellidos}"
                            : null,
                        TotalClases = totalClases,
                        TotalAsistencias = totalAsistencias,
                        TotalFaltas = totalFaltas,
                        PorcentajeAsistencia = Math.Round(porcentaje, 2),
                        AlertaBajaAsistencia = porcentaje < 70,
                        Asistencias = asistencias.Select(a => new AsistenciaDetalleDto
                        {
                            Id = a.Id,
                            Fecha = a.Fecha,
                            Presente = a.Presente,
                            TipoClase = a.TipoClase,
                            Observaciones = a.Observaciones
                        }).ToList()
                    });
                }

                return resultado.OrderBy(r => r.NombreCurso).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias por estudiante");
                throw;
            }
        }

        public async Task<ResumenAsistenciaEstudianteDto> GetResumenAsistenciaEstudianteCursoAsync(int idEstudiante, int idCurso)
        {
            try
            {
                var estudiante = await _context.Estudiantes.FindAsync(idEstudiante);
                if (estudiante == null)
                {
                    throw new ArgumentException($"Estudiante con ID {idEstudiante} no encontrado");
                }

                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    throw new ArgumentException($"Curso con ID {idCurso} no encontrado");
                }

                var asistencias = await _context.Asistencias
                    .Where(a => a.IdEstudiante == idEstudiante && a.IdCurso == idCurso)
                    .OrderBy(a => a.Fecha)
                    .ToListAsync();

                var totalClases = asistencias.Count;
                var totalAsistencias = asistencias.Count(a => a.Presente);
                var totalFaltas = asistencias.Count(a => !a.Presente);
                var porcentaje = totalClases > 0 ? (decimal)totalAsistencias / totalClases * 100 : 0;

                return new ResumenAsistenciaEstudianteDto
                {
                    IdEstudiante = idEstudiante,
                    NombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                    CodigoEstudiante = estudiante.Codigo,
                    IdCurso = idCurso,
                    NombreCurso = curso.NombreCurso,
                    TotalClases = totalClases,
                    TotalAsistencias = totalAsistencias,
                    TotalFaltas = totalFaltas,
                    PorcentajeAsistencia = Math.Round(porcentaje, 2),
                    Detalles = asistencias.Select(a => new AsistenciaDetalleDto
                    {
                        Id = a.Id,
                        Fecha = a.Fecha,
                        Presente = a.Presente,
                        Observaciones = a.Observaciones
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencia estudiante-curso");
                throw;
            }
        }

        public async Task<EstadisticasAsistenciaEstudianteDto> GetEstadisticasAsistenciaEstudianteAsync(int idEstudiante, int? idPeriodo = null)
        {
            try
            {
                var cursosPorAsistencia = await GetAsistenciasPorEstudianteAsync(idEstudiante, idPeriodo);

                var totalCursos = cursosPorAsistencia.Count;
                var totalClases = cursosPorAsistencia.Sum(c => c.TotalClases);
                var totalAsistencias = cursosPorAsistencia.Sum(c => c.TotalAsistencias);
                var totalFaltas = cursosPorAsistencia.Sum(c => c.TotalFaltas);
                var porcentajeGeneral = totalClases > 0 ? (decimal)totalAsistencias / totalClases * 100 : 0;
                var cursosConAlerta = cursosPorAsistencia.Count(c => c.AlertaBajaAsistencia);

                return new EstadisticasAsistenciaEstudianteDto
                {
                    TotalCursos = totalCursos,
                    TotalClases = totalClases,
                    TotalAsistencias = totalAsistencias,
                    TotalFaltas = totalFaltas,
                    PorcentajeAsistenciaGeneral = Math.Round(porcentajeGeneral, 2),
                    CursosConAlerta = cursosConAlerta,
                    CursosPorAsistencia = cursosPorAsistencia
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de asistencia del estudiante");
                throw;
            }
        }

        public async Task<List<TendenciaAsistenciaDto>> GetTendenciaAsistenciaEstudianteAsync(int idEstudiante, int meses = 6)
        {
            try
            {
                var fechaLimite = DateTime.Now.AddMonths(-meses);

                var asistencias = await _context.Asistencias
                    .Where(a => a.IdEstudiante == idEstudiante && a.Fecha >= fechaLimite)
                    .ToListAsync();

                var tendencias = asistencias
                    .GroupBy(a => new { a.Fecha.Year, a.Fecha.Month })
                    .Select(g => new TendenciaAsistenciaDto
                    {
                        Anio = g.Key.Year,
                        NumeroMes = g.Key.Month,
                        Mes = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new CultureInfo("es-ES")),
                        TotalClases = g.Count(),
                        TotalAsistencias = g.Count(a => a.Presente),
                        PorcentajeAsistencia = g.Any() ? Math.Round((decimal)g.Count(a => a.Presente) / g.Count() * 100, 2) : 0
                    })
                    .OrderBy(t => t.Anio)
                    .ThenBy(t => t.NumeroMes)
                    .ToList();

                return tendencias;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tendencia de asistencia del estudiante");
                throw;
            }
        }

        // ============================================
        // IMPLEMENTACIÓN MÉTODOS AUXILIARES
        // ============================================

        public async Task<bool> ExisteAsistenciaAsync(int idEstudiante, int idCurso, DateTime fecha)
        {
            return await _context.Asistencias
                .AnyAsync(a =>
                    a.IdEstudiante == idEstudiante &&
                    a.IdCurso == idCurso &&
                    a.Fecha.Date == fecha.Date);
        }

        public async Task<decimal> CalcularPorcentajeAsistenciaAsync(int idEstudiante, int idCurso)
        {
            var asistencias = await _context.Asistencias
                .Where(a => a.IdEstudiante == idEstudiante && a.IdCurso == idCurso)
                .ToListAsync();

            if (!asistencias.Any())
                return 0;

            var totalAsistencias = asistencias.Count(a => a.Presente);
            return Math.Round((decimal)totalAsistencias / asistencias.Count * 100, 2);
        }

        public async Task<List<AsistenciaDto>> GetAsistenciasByEstudianteAsync(int idEstudiante)
        {
            var asistencias = await _context.Asistencias
                .Include(a => a.Estudiante)
                .Include(a => a.Curso)
                .Where(a => a.IdEstudiante == idEstudiante)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();

            return asistencias.Select(a => new AsistenciaDto
            {
                Id = a.Id,
                IdEstudiante = a.IdEstudiante,
                NombreEstudiante = $"{a.Estudiante?.Nombres} {a.Estudiante?.Apellidos}",
                IdCurso = a.IdCurso,
                NombreCurso = a.Curso?.NombreCurso ?? "",
                Fecha = a.Fecha,
                Presente = a.Presente,
                Observaciones = a.Observaciones,
                FechaRegistro = a.FechaRegistro
            }).ToList();
        }

        public async Task<List<AsistenciaDto>> GetAsistenciasByCursoAsync(int idCurso)
        {
            var asistencias = await _context.Asistencias
                .Include(a => a.Estudiante)
                .Include(a => a.Curso)
                .Where(a => a.IdCurso == idCurso)
                .OrderByDescending(a => a.Fecha)
                .ThenBy(a => a.Estudiante!.Apellidos)
                .ToListAsync();

            return asistencias.Select(a => new AsistenciaDto
            {
                Id = a.Id,
                IdEstudiante = a.IdEstudiante,
                NombreEstudiante = $"{a.Estudiante?.Nombres} {a.Estudiante?.Apellidos}",
                IdCurso = a.IdCurso,
                NombreCurso = a.Curso?.NombreCurso ?? "",
                Fecha = a.Fecha,
                Presente = a.Presente,
                Observaciones = a.Observaciones,
                FechaRegistro = a.FechaRegistro
            }).ToList();
        }
    }
}
