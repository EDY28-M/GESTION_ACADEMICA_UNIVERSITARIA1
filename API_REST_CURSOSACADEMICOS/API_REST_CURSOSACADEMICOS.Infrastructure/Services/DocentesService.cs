using System.Globalization;
using System.Text;
using System.Text.Json;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Data;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public sealed class DocentesService : IDocentesService
{
    private readonly GestionAcademicaContext _context;
    private readonly IAsistenciaService _asistenciaService;

    public DocentesService(GestionAcademicaContext context, IAsistenciaService asistenciaService)
    {
        _context = context;
        _asistenciaService = asistenciaService;
    }

    public async Task<List<DocenteDto>> GetDocentesAsync()
    {
        return await _context.Docentes
            .Include(d => d.Cursos)
            .Select(d => new DocenteDto
            {
                Id = d.Id,
                Apellidos = d.Apellidos,
                Nombres = d.Nombres,
                Profesion = d.Profesion,
                FechaNacimiento = d.FechaNacimiento,
                Correo = d.Correo,
                Cursos = d.Cursos.Select(c => new CursoSimpleDto
                {
                    Id = c.Id,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    Ciclo = c.Ciclo
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<DocenteDto?> GetDocenteAsync(int id)
    {
        return await _context.Docentes
            .Include(d => d.Cursos)
            .Where(d => d.Id == id)
            .Select(d => new DocenteDto
            {
                Id = d.Id,
                Apellidos = d.Apellidos,
                Nombres = d.Nombres,
                Profesion = d.Profesion,
                FechaNacimiento = d.FechaNacimiento,
                Correo = d.Correo,
                Cursos = d.Cursos.Select(c => new CursoSimpleDto
                {
                    Id = c.Id,
                    NombreCurso = c.NombreCurso,
                    Creditos = c.Creditos,
                    HorasSemanal = c.HorasSemanal,
                    Ciclo = c.Ciclo
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool success, string? error, DocenteDto? created)> CreateDocenteAsync(DocenteCreateDto docenteDto)
    {
        if (!string.IsNullOrEmpty(docenteDto.Correo))
        {
            var existeCorreo = await _context.Docentes.AnyAsync(d => d.Correo == docenteDto.Correo);
            if (existeCorreo)
            {
                return (false, "Ya existe un docente con este correo electrónico", null);
            }
        }

        var docente = new Docente
        {
            Apellidos = docenteDto.Apellidos,
            Nombres = docenteDto.Nombres,
            Profesion = docenteDto.Profesion,
            FechaNacimiento = docenteDto.FechaNacimiento,
            Correo = docenteDto.Correo
        };

        _context.Docentes.Add(docente);
        await _context.SaveChangesAsync();

        var docenteResponse = new DocenteDto
        {
            Id = docente.Id,
            Apellidos = docente.Apellidos,
            Nombres = docente.Nombres,
            Profesion = docente.Profesion,
            FechaNacimiento = docente.FechaNacimiento,
            Correo = docente.Correo,
            Cursos = new List<CursoSimpleDto>()
        };

        return (true, null, docenteResponse);
    }

    public async Task<(bool notFound, bool success, string? error)> UpdateDocenteAsync(int id, DocenteUpdateDto docenteDto)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
        {
            return (true, false, $"Docente con ID {id} no encontrado");
        }

        if (!string.IsNullOrEmpty(docenteDto.Correo))
        {
            var existeCorreo = await _context.Docentes.AnyAsync(d => d.Correo == docenteDto.Correo && d.Id != id);
            if (existeCorreo)
            {
                return (false, false, "Ya existe otro docente con este correo electrónico");
            }
        }

        docente.Apellidos = docenteDto.Apellidos;
        docente.Nombres = docenteDto.Nombres;
        docente.Profesion = docenteDto.Profesion;
        docente.FechaNacimiento = docenteDto.FechaNacimiento;
        docente.Correo = docenteDto.Correo;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _context.Docentes.AnyAsync(e => e.Id == id);
            if (!exists)
            {
                return (true, false, "Docente no encontrado");
            }

            throw;
        }

        return (false, true, null);
    }

    public async Task<(bool notFound, bool hasAssignedCourses, bool success, string? error)> DeleteDocenteAsync(int id)
    {
        var docente = await _context.Docentes
            .Include(d => d.Cursos)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (docente == null)
        {
            return (true, false, false, $"Docente con ID {id} no encontrado");
        }

        if (docente.Cursos.Any())
        {
            return (false, true, false, "No se puede eliminar el docente porque tiene cursos asignados");
        }

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync();
        return (false, false, true, null);
    }

    public async Task<List<CursoDocenteDto>> GetMisCursosAsync(int docenteId)
    {
        // Obtener período activo
        var periodoActivo = await _context.Periodos
            .AsNoTracking()
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();

        if (periodoActivo == null)
        {
            return new List<CursoDocenteDto>();
        }

        // Obtener cursos del docente
        var cursos = await _context.Cursos
            .AsNoTracking()
            .Where(c => c.IdDocente == docenteId)
            .ToListAsync();

        if (!cursos.Any())
        {
            return new List<CursoDocenteDto>();
        }

        var cursosIds = cursos.Select(c => c.Id).ToList();

        // Traer todas las matrículas (con notas) de los cursos del docente en una consulta
        var matriculasConNotas = await _context.Matriculas
            .AsNoTracking()
            .Where(m => cursosIds.Contains(m.IdCurso) &&
                        m.IdPeriodo == periodoActivo.Id &&
                        m.Estado == "Matriculado")
            .Include(m => m.Notas)
            .ToListAsync();

        // Traer asistencias (agregadas) en una consulta
        var asistencias = await _context.Asistencias
            .AsNoTracking()
            .Where(a => cursosIds.Contains(a.IdCurso))
            .GroupBy(a => a.IdCurso)
            .Select(g => new
            {
                IdCurso = g.Key,
                TotalRegistros = g.Count(),
                Presentes = g.Count(a => a.Presente)
            })
            .ToListAsync();

        var cursosDto = cursos.Select(c =>
        {
            var matriculasCurso = matriculasConNotas.Where(m => m.IdCurso == c.Id).ToList();
            var asistenciaCurso = asistencias.FirstOrDefault(a => a.IdCurso == c.Id);

            var promedios = matriculasCurso
                .Where(m => m.Notas.Any())
                .Select(m => m.Notas.Sum(n => n.NotaValor * n.Peso / 100m))
                .Where(p => p > 0)
                .ToList();

            return new CursoDocenteDto
            {
                Id = c.Id,
                NombreCurso = c.NombreCurso,
                Creditos = c.Creditos,
                HorasSemanal = c.HorasSemanal,
                Ciclo = c.Ciclo,
                PeriodoActualId = periodoActivo.Id,
                PeriodoNombre = periodoActivo.Nombre,
                TotalEstudiantes = matriculasCurso.Count,
                PromedioGeneral = promedios.Any() ? promedios.Average() : 0,
                PorcentajeAsistenciaPromedio = asistenciaCurso != null && asistenciaCurso.TotalRegistros > 0
                    ? (decimal)asistenciaCurso.Presentes / asistenciaCurso.TotalRegistros * 100
                    : 0
            };
        }).ToList();

        return cursosDto;
    }

    public async Task<ServiceOutcome> GetEstudiantesCursoAsync(int docenteId, int idCurso)
    {
        // Verificar que el curso pertenece al docente
        var curso = await _context.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == idCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        // Obtener período activo
        var periodoActivo = await _context.Periodos
            .AsNoTracking()
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();

        if (periodoActivo == null)
        {
            return ServiceOutcome.Ok(new
            {
                estudiantes = new List<EstudianteCursoDto>(),
                mensaje = "No hay período activo. Configure un período activo en el sistema.",
                hayPeriodoActivo = false
            });
        }

        // Una sola consulta con matrículas, estudiante y notas
        var matriculas = await _context.Matriculas
            .AsNoTracking()
            .Where(m => m.IdCurso == idCurso &&
                        m.IdPeriodo == periodoActivo.Id &&
                        m.Estado == "Matriculado")
            .Include(m => m.Estudiante)
            .Include(m => m.Notas)
            .ToListAsync();

        if (matriculas.Count == 0)
        {
            var totalMatriculasOtrosPeriodos = await _context.Matriculas.CountAsync(m => m.IdCurso == idCurso);

            return ServiceOutcome.Ok(new
            {
                estudiantes = new List<EstudianteCursoDto>(),
                mensaje = totalMatriculasOtrosPeriodos > 0
                    ? $"No hay estudiantes matriculados en este curso para el período activo '{periodoActivo.Nombre}'."
                    : "No hay estudiantes matriculados en este curso.",
                hayPeriodoActivo = true,
                periodoActivo = periodoActivo.Nombre,
                totalMatriculasOtrosPeriodos
            });
        }

        var estudiantesIds = matriculas.Select(m => m.IdEstudiante).ToList();
        var asistenciasPorEstudiante = await _context.Asistencias
            .AsNoTracking()
            .Where(a => a.IdCurso == idCurso && estudiantesIds.Contains(a.IdEstudiante))
            .GroupBy(a => a.IdEstudiante)
            .Select(g => new
            {
                IdEstudiante = g.Key,
                Total = g.Count(),
                Presentes = g.Count(a => a.Presente)
            })
            .ToListAsync();

        var estudiantes = matriculas
            .Select(m =>
            {
                var asistencia = asistenciasPorEstudiante.FirstOrDefault(a => a.IdEstudiante == m.IdEstudiante);
                var promedio = m.Notas.Any()
                    ? Math.Round(m.Notas.Sum(n => n.NotaValor * n.Peso / 100m), 0, MidpointRounding.AwayFromZero)
                    : (decimal?)null;

                var notasDict = new Dictionary<string, object>();
                foreach (var nota in m.Notas)
                {
                    // Usar el nombre del tipo de evaluación como clave (case-insensitive para evitar problemas)
                    var tipoEvaluacionKey = nota.TipoEvaluacion.Trim();
                    notasDict[tipoEvaluacionKey] = nota.NotaValor;
                }

                if (promedio.HasValue)
                {
                    notasDict["promedioCalculado"] = m.Notas.Sum(n => n.NotaValor * n.Peso / 100m);
                    notasDict["promedioFinal"] = promedio.Value;
                }

                return new EstudianteCursoDto
                {
                    Id = m.Id,
                    IdEstudiante = m.IdEstudiante,
                    NombreCompleto = $"{m.Estudiante!.Nombres} {m.Estudiante.Apellidos}",
                    Codigo = m.Estudiante.Codigo,
                    Correo = m.Estudiante.Correo,
                    IdMatricula = m.Id,
                    EstadoMatricula = m.Estado,
                    PromedioFinal = promedio,
                    PorcentajeAsistencia = asistencia != null && asistencia.Total > 0
                        ? (decimal)asistencia.Presentes / asistencia.Total * 100
                        : 0,
                    Notas = m.Notas.Any() ? notasDict : null
                };
            })
            .OrderBy(e => e.NombreCompleto)
            .ToList();

        return ServiceOutcome.Ok(estudiantes);
    }

    public async Task<ServiceOutcome> RegistrarNotasAsync(int docenteId, int idCurso, JsonElement notasJson)
    {
        try
        {
            // Verificar que el curso pertenece al docente
            var curso = await _context.Cursos.FindAsync(idCurso);
            if (curso == null)
            {
                return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
            }

            if (curso.IdDocente != docenteId)
            {
                return ServiceOutcome.Forbidden();
            }

            // Extraer idMatricula del JSON
            if (!notasJson.TryGetProperty("idMatricula", out var idMatriculaElement))
            {
                return ServiceOutcome.BadRequest(new { message = "idMatricula es requerido" });
            }

            int idMatricula = idMatriculaElement.GetInt32();

            // Verificar que la matrícula existe y pertenece al curso
            var matricula = await _context.Matriculas
                .Include(m => m.Notas)
                .FirstOrDefaultAsync(m => m.Id == idMatricula && m.IdCurso == idCurso);

            if (matricula == null)
            {
                return ServiceOutcome.NotFound(new { message = "Matrícula no encontrada" });
            }

            // Obtener observaciones si existen
            string? observaciones = null;
            if (notasJson.TryGetProperty("observaciones", out var obsElement))
            {
                observaciones = obsElement.GetString();
            }

            // Obtener tipos de evaluación configurados para el curso
            var tiposEvaluacion = await _context.TiposEvaluacion
                .Where(t => t.IdCurso == idCurso && t.Activo)
                .OrderBy(t => t.Orden)
                .ToListAsync();

            // Si no hay tipos configurados, usar configuración por defecto
            if (tiposEvaluacion.Count == 0)
            {
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Parcial 1", "parcial1", notasJson, 10, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Parcial 2", "parcial2", notasJson, 10, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Prácticas", "practicas", notasJson, 20, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Medio Curso", "medioCurso", notasJson, 20, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Examen Final", "examenFinal", notasJson, 20, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Actitud", "actitud", notasJson, 5, observaciones);
                await RegistrarOActualizarNotaDesdeJson(matricula.Id, "Trabajos", "trabajos", notasJson, 15, observaciones);
            }
            else
            {
                foreach (var tipoEval in tiposEvaluacion)
                {
                    var nombreTipo = tipoEval.Nombre;

                    if (notasJson.TryGetProperty(nombreTipo, out var valorElement))
                    {
                        decimal? valor = null;
                        if (valorElement.ValueKind == JsonValueKind.Number)
                        {
                            valor = valorElement.GetDecimal();
                        }

                        await RegistrarOActualizarNota(
                            matricula.Id,
                            nombreTipo,
                            valor,
                            tipoEval.Peso,
                            observaciones
                        );
                    }
                }
            }

            await _context.SaveChangesAsync();

            var promedioFinal = CalcularPromedioFinalConRedondeo(matricula.Id);

            return ServiceOutcome.Ok(new
            {
                message = "Notas registradas correctamente",
                promedioFinal,
                aprobado = promedioFinal >= 11
            });
        }
        catch (InvalidOperationException ex)
        {
            // Capturar específicamente el error de bloqueo de examen final por asistencias
            return ServiceOutcome.BadRequest(new { message = ex.Message });
        }
    }

    public async Task<ServiceOutcome> ObtenerTiposEvaluacionAsync(int docenteId, int idCurso)
    {
        var curso = await _context.Cursos.FindAsync(idCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        var tiposEvaluacion = await _context.TiposEvaluacion
            .Where(t => t.IdCurso == idCurso && t.Activo)
            .OrderBy(t => t.Orden)
            .Select(t => new TipoEvaluacionDto
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Peso = t.Peso,
                Orden = t.Orden,
                Activo = t.Activo
            })
            .ToListAsync();

        if (tiposEvaluacion.Count == 0)
        {
            tiposEvaluacion = new List<TipoEvaluacionDto>
            {
                new() { Id = 0, Nombre = "Parcial 1", Peso = 10, Orden = 1, Activo = true },
                new() { Id = 0, Nombre = "Parcial 2", Peso = 10, Orden = 2, Activo = true },
                new() { Id = 0, Nombre = "Prácticas", Peso = 20, Orden = 3, Activo = true },
                new() { Id = 0, Nombre = "Medio Curso", Peso = 20, Orden = 4, Activo = true },
                new() { Id = 0, Nombre = "Examen Final", Peso = 20, Orden = 5, Activo = true },
                new() { Id = 0, Nombre = "Actitud", Peso = 5, Orden = 6, Activo = true },
                new() { Id = 0, Nombre = "Trabajos", Peso = 15, Orden = 7, Activo = true }
            };
        }

        return ServiceOutcome.Ok(tiposEvaluacion);
    }

    public async Task<ServiceOutcome> ConfigurarTiposEvaluacionAsync(int docenteId, int idCurso, ConfigurarTiposEvaluacionDto configDto)
    {
        var curso = await _context.Cursos.FindAsync(idCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        var pesoTotal = configDto.TiposEvaluacion.Where(t => t.Activo).Sum(t => t.Peso);
        if (Math.Abs(pesoTotal - 100) > 0.01m)
        {
            return ServiceOutcome.BadRequest(new { message = $"Los pesos deben sumar 100%. Suma actual: {pesoTotal}%" });
        }

        var tiposExistentes = await _context.TiposEvaluacion
            .Where(t => t.IdCurso == idCurso)
            .ToListAsync();

        bool esPrimeraConfiguracion = tiposExistentes.Count == 0;
        if (esPrimeraConfiguracion)
        {
            var matriculasCurso = await _context.Matriculas
                .Where(m => m.IdCurso == idCurso)
                .Select(m => m.Id)
                .ToListAsync();

            var mapeoMigracion = new Dictionary<int, List<string>>
            {
                { 0, new List<string> { "Parcial 1", "parcial 1", "EP1", "Examen Parcial 1" } },
                { 1, new List<string> { "Parcial 2", "parcial 2", "EP2", "Examen Parcial 2" } },
                { 2, new List<string> { "Prácticas", "Práctica", "practicas", "practica", "PR" } },
                { 3, new List<string> { "Medio Curso", "medio curso", "MedioCurso", "MC" } },
                { 4, new List<string> { "Examen Final", "examen final", "ExamenFinal", "EF" } },
                { 5, new List<string> { "Actitud", "actitud", "EA" } },
                { 6, new List<string> { "Trabajos", "trabajos", "Trabajo encargado", "trabajo encargado", "TE", "T" } }
            };

            for (int i = 0; i < configDto.TiposEvaluacion.Count && i < mapeoMigracion.Count; i++)
            {
                var tipoDto = configDto.TiposEvaluacion[i];
                var nombresAntiguos = mapeoMigracion[i];

                var notasAMigrar = await _context.Notas
                    .Where(n => matriculasCurso.Contains(n.IdMatricula) &&
                                nombresAntiguos.Contains(n.TipoEvaluacion))
                    .ToListAsync();

                foreach (var nota in notasAMigrar)
                {
                    nota.TipoEvaluacion = tipoDto.Nombre;
                    nota.Peso = tipoDto.Peso;
                }
            }
        }

        foreach (var tipoDto in configDto.TiposEvaluacion)
        {
            if (tipoDto.Id == 0)
            {
                var nuevoTipo = new TipoEvaluacion
                {
                    IdCurso = idCurso,
                    Nombre = tipoDto.Nombre,
                    Peso = tipoDto.Peso,
                    Orden = tipoDto.Orden,
                    Activo = tipoDto.Activo
                };
                _context.TiposEvaluacion.Add(nuevoTipo);
            }
            else
            {
                var tipoExistente = tiposExistentes.FirstOrDefault(t => t.Id == tipoDto.Id);
                if (tipoExistente != null)
                {
                    var nombreAnterior = tipoExistente.Nombre;
                    var nombreNuevo = tipoDto.Nombre;
                    var pesoAnterior = tipoExistente.Peso;
                    var pesoNuevo = tipoDto.Peso;

                    var matriculasCurso = await _context.Matriculas
                        .Where(m => m.IdCurso == idCurso)
                        .Select(m => m.Id)
                        .ToListAsync();

                    if (nombreAnterior != nombreNuevo)
                    {
                        var notasAActualizar = await _context.Notas
                            .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == nombreAnterior)
                            .ToListAsync();

                        foreach (var nota in notasAActualizar)
                        {
                            nota.TipoEvaluacion = nombreNuevo;
                            nota.Peso = pesoNuevo;
                        }
                    }
                    else if (Math.Abs(pesoAnterior - pesoNuevo) > 0.01m)
                    {
                        var notasAActualizar = await _context.Notas
                            .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == nombreAnterior)
                            .ToListAsync();

                        foreach (var nota in notasAActualizar)
                        {
                            nota.Peso = pesoNuevo;
                        }
                    }

                    tipoExistente.Nombre = nombreNuevo;
                    tipoExistente.Peso = pesoNuevo;
                    tipoExistente.Orden = tipoDto.Orden;
                    tipoExistente.Activo = tipoDto.Activo;
                }
            }
        }

        var idsConfiguracion = configDto.TiposEvaluacion.Where(t => t.Id > 0).Select(t => t.Id).ToList();
        var tiposAEliminar = tiposExistentes.Where(t => !idsConfiguracion.Contains(t.Id)).ToList();

        if (tiposAEliminar.Any())
        {
            var matriculasCurso = await _context.Matriculas
                .Where(m => m.IdCurso == idCurso)
                .Select(m => m.Id)
                .ToListAsync();

            foreach (var tipoEliminar in tiposAEliminar)
            {
                var notasAEliminar = await _context.Notas
                    .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == tipoEliminar.Nombre)
                    .ToListAsync();

                _context.Notas.RemoveRange(notasAEliminar);
            }
        }

        _context.TiposEvaluacion.RemoveRange(tiposAEliminar);

        await _context.SaveChangesAsync();
        return ServiceOutcome.Ok(new { message = "Tipos de evaluación configurados correctamente" });
    }

    public async Task<ServiceOutcome> RegistrarAsistenciaAsync(int docenteId, RegistrarAsistenciasMasivasDto asistenciaDto)
    {
        var curso = await _context.Cursos.FindAsync(asistenciaDto.IdCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        foreach (var asistencia in asistenciaDto.Estudiantes)
        {
            var asistenciaExistente = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.IdEstudiante == asistencia.IdEstudiante &&
                                         a.IdCurso == asistenciaDto.IdCurso &&
                                         a.Fecha.Date == asistenciaDto.Fecha.Date &&
                                         a.TipoClase == asistenciaDto.TipoClase);

            if (asistenciaExistente != null)
            {
                asistenciaExistente.Presente = asistencia.Presente;
                asistenciaExistente.Observaciones = asistencia.Observaciones;
                asistenciaExistente.FechaRegistro = DateTime.Now;
            }
            else
            {
                _context.Asistencias.Add(new Asistencia
                {
                    IdEstudiante = asistencia.IdEstudiante,
                    IdCurso = asistenciaDto.IdCurso,
                    Fecha = asistenciaDto.Fecha.Date,
                    Presente = asistencia.Presente,
                    TipoClase = asistenciaDto.TipoClase,
                    Observaciones = asistencia.Observaciones,
                    FechaRegistro = DateTime.Now
                });
            }
        }

        await _context.SaveChangesAsync();
        return ServiceOutcome.Ok(new { message = "Asistencias registradas correctamente" });
    }

    public async Task<ServiceOutcome> GetAsistenciaCursoAsync(int docenteId, int idCurso, DateTime? fecha, string? tipoClase)
    {
        var curso = await _context.Cursos.FindAsync(idCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        var query = _context.Asistencias.Where(a => a.IdCurso == idCurso);

        if (fecha.HasValue)
        {
            query = query.Where(a => a.Fecha.Date == fecha.Value.Date);
        }

        if (!string.IsNullOrEmpty(tipoClase))
        {
            query = query.Where(a => a.TipoClase == tipoClase);
        }

        var asistencias = await query
            .Include(a => a.Estudiante)
            .ToListAsync();

        var resultado = asistencias
            .Select(a => new AsistenciaDto
            {
                Id = a.Id,
                IdEstudiante = a.IdEstudiante,
                NombreEstudiante = $"{a.Estudiante!.Nombres} {a.Estudiante.Apellidos}",
                IdCurso = a.IdCurso,
                NombreCurso = curso.NombreCurso,
                Fecha = a.Fecha,
                Presente = a.Presente,
                Observaciones = a.Observaciones,
                FechaRegistro = a.FechaRegistro,
                TipoClase = a.TipoClase
            })
            .OrderByDescending(a => a.Fecha)
            .ThenBy(a => a.NombreEstudiante)
            .ToList();

        return ServiceOutcome.Ok(resultado);
    }

    public async Task<ServiceOutcome> GetResumenAsistenciaAsync(int docenteId, int idCurso)
    {
        var curso = await _context.Cursos.FindAsync(idCurso);
        if (curso == null)
        {
            return ServiceOutcome.NotFound(new { message = "Curso no encontrado" });
        }

        if (curso.IdDocente != docenteId)
        {
            return ServiceOutcome.Forbidden();
        }

        var periodoActivo = await _context.Periodos
            .Where(p => p.Activo == true)
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefaultAsync();

        if (periodoActivo == null)
        {
            return ServiceOutcome.Ok(new List<ResumenAsistenciaDto>());
        }

        var estudiantesIds = await _context.Matriculas
            .Where(m => m.IdCurso == idCurso &&
                        m.IdPeriodo == periodoActivo.Id &&
                        m.Estado == "Matriculado")
            .Select(m => m.IdEstudiante)
            .ToListAsync();

        var resumen = new List<ResumenAsistenciaDto>();

        foreach (var estudianteId in estudiantesIds)
        {
            var estudiante = await _context.Estudiantes.FindAsync(estudianteId);
            if (estudiante == null) continue;

            var asistencias = await _context.Asistencias
                .Where(a => a.IdEstudiante == estudianteId && a.IdCurso == idCurso)
                .OrderBy(a => a.Fecha)
                .Select(a => new AsistenciaDto
                {
                    Id = a.Id,
                    IdEstudiante = a.IdEstudiante,
                    NombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                    IdCurso = a.IdCurso,
                    NombreCurso = curso.NombreCurso,
                    Fecha = a.Fecha,
                    Presente = a.Presente,
                    Observaciones = a.Observaciones,
                    FechaRegistro = a.FechaRegistro
                })
                .ToListAsync();

            var totalClases = asistencias.Count;
            var presentes = asistencias.Count(a => a.Presente);
            var faltas = totalClases - presentes;
            var porcentaje = totalClases > 0 ? (decimal)presentes / totalClases * 100 : 0;

            resumen.Add(new ResumenAsistenciaDto
            {
                IdEstudiante = estudianteId,
                NombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                IdCurso = idCurso,
                NombreCurso = curso.NombreCurso,
                TotalAsistencias = totalClases,
                AsistenciasPresente = presentes,
                AsistenciasFalta = faltas,
                PorcentajeAsistencia = porcentaje,
                Asistencias = asistencias
            });
        }

        return ServiceOutcome.Ok(resumen.OrderBy(r => r.NombreEstudiante).ToList());
    }

    #region Métodos auxiliares (migrados desde DocentesController)

    private async Task RegistrarOActualizarNotaDesdeJson(
        int idMatricula,
        string tipoEvaluacion,
        string campoJson,
        JsonElement notasJson,
        int peso,
        string? observaciones)
    {
        var valor = ObtenerValorNotaDesdeJson(notasJson, campoJson, tipoEvaluacion);
        await RegistrarOActualizarNota(idMatricula, tipoEvaluacion, valor, peso, observaciones);
    }

    private decimal? ObtenerValorNotaDesdeJson(
        JsonElement notasJson,
        string? clavePreferida,
        string tipoEvaluacion)
    {
        var objetivos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(clavePreferida))
        {
            objetivos.Add(NormalizarClaveJson(clavePreferida));
        }

        if (!string.IsNullOrWhiteSpace(tipoEvaluacion))
        {
            objetivos.Add(NormalizarClaveJson(tipoEvaluacion));

            var camel = ConvertirACamelCase(tipoEvaluacion);
            objetivos.Add(NormalizarClaveJson(camel));
        }

        foreach (var propiedad in notasJson.EnumerateObject())
        {
            if (propiedad.Value.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var nombreNormalizado = NormalizarClaveJson(propiedad.Name);
            if (objetivos.Contains(nombreNormalizado))
            {
                return propiedad.Value.GetDecimal();
            }
        }

        return null;
    }

    private string NormalizarClaveJson(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return string.Empty;
        }

        var baseTexto = RemoverDiacriticos(clave);

        baseTexto = baseTexto
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Trim();

        return baseTexto.ToLowerInvariant();
    }

    private string RemoverDiacriticos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var caracteres = normalizado
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(caracteres).Normalize(NormalizationForm.FormC);
    }

    private async Task RegistrarOActualizarNota(int idMatricula, string tipoEvaluacion, decimal? notaValor, decimal peso, string? observaciones)
    {
        if (!notaValor.HasValue) return;

        // Validación especial para Examen Final: verificar asistencias
        if (tipoEvaluacion.Equals("Examen Final", StringComparison.OrdinalIgnoreCase))
        {
            // Obtener el estudiante e idCurso de la matrícula
            var matricula = await _context.Matriculas
                .Include(m => m.Estudiante)
                .Include(m => m.Curso)
                .FirstOrDefaultAsync(m => m.Id == idMatricula);

            if (matricula != null)
            {
                // Verificar si puede dar examen final según asistencias
                var puedeRendirExamen = await _asistenciaService.PuedeDarExamenFinalAsync(
                    matricula.IdEstudiante, 
                    matricula.IdCurso
                );

                if (!puedeRendirExamen)
                {
                    var estadisticas = await _asistenciaService.CalcularEstadisticasAsistenciaAsync(
                        matricula.IdEstudiante, 
                        matricula.IdCurso
                    );
                    
                    throw new InvalidOperationException(
                        $"No se puede registrar la nota de Examen Final. {estadisticas.MensajeBloqueo}"
                    );
                }
            }
        }

        var notaExistente = await _context.Notas
            .FirstOrDefaultAsync(n => n.IdMatricula == idMatricula && n.TipoEvaluacion == tipoEvaluacion);

        if (notaExistente != null)
        {
            notaExistente.NotaValor = notaValor.Value;
            notaExistente.Peso = peso;
            notaExistente.Fecha = DateTime.Now;
            notaExistente.Observaciones = observaciones;
        }
        else
        {
            _context.Notas.Add(new Nota
            {
                IdMatricula = idMatricula,
                TipoEvaluacion = tipoEvaluacion,
                NotaValor = notaValor.Value,
                Peso = peso,
                Fecha = DateTime.Now,
                Observaciones = observaciones
            });
        }
    }

    private decimal CalcularPromedioMatricula(int idMatricula)
    {
        var matricula = _context.Matriculas.Find(idMatricula);
        if (matricula == null || matricula.Estado != "Matriculado")
        {
            return 0;
        }

        var notas = _context.Notas.Where(n => n.IdMatricula == idMatricula).ToList();
        if (notas.Count == 0) return 0;

        decimal promedioPonderado = 0;
        decimal pesoTotal = 0;

        foreach (var nota in notas)
        {
            var contribucion = nota.NotaValor * (nota.Peso / 100m);
            promedioPonderado += contribucion;
            pesoTotal += nota.Peso;
        }

        _ = pesoTotal; // compat / debug (no se usa, se mantiene el comportamiento)
        return promedioPonderado;
    }

    private decimal? CalcularPromedioFinalConRedondeo(int idMatricula)
    {
        var promedio = CalcularPromedioMatricula(idMatricula);
        if (promedio == 0) return null;

        return Math.Round(promedio, 0, MidpointRounding.AwayFromZero);
    }

    private string ConvertirACamelCase(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return texto;

        var palabras = texto.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 0) return texto.ToLower();

        var resultado = palabras[0].ToLower();
        for (int i = 1; i < palabras.Length; i++)
        {
            if (!string.IsNullOrEmpty(palabras[i]))
            {
                resultado += char.ToUpper(palabras[i][0]) + palabras[i].Substring(1).ToLower();
            }
        }

        return resultado;
    }

    #endregion
}


