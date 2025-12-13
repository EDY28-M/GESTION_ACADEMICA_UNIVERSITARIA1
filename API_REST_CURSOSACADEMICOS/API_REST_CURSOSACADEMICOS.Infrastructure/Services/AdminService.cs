using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public sealed class AdminService : IAdminService
{
    private readonly GestionAcademicaContext _context;
    private readonly IEstudianteService _estudianteService;

    public AdminService(GestionAcademicaContext context, IEstudianteService estudianteService)
    {
        _context = context;
        _estudianteService = estudianteService;
    }

    public async Task<ServiceOutcome> GetTodosEstudiantesAsync()
    {
        var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);

        var estudiantes = await _context.Estudiantes
            .Include(e => e.Matriculas.Where(m => periodoActivo != null && m.IdPeriodo == periodoActivo.Id))
                .ThenInclude(m => m.Curso)
            .Select(e => new
            {
                id = e.Id,
                codigo = e.Codigo,
                nombres = e.Nombres,
                apellidos = e.Apellidos,
                nombreCompleto = e.Nombres + " " + e.Apellidos,
                email = e.Correo ?? "",
                dni = e.Dni ?? "",
                cicloActual = e.CicloActual,
                carrera = e.Carrera,
                estado = e.Estado,
                creditosAcumulados = e.CreditosAcumulados,
                promedioAcumulado = e.PromedioAcumulado,
                promedioSemestral = e.PromedioSemestral,
                creditosSemestreActual = periodoActivo != null
                    ? e.Matriculas
                        .Where(m => m.IdPeriodo == periodoActivo.Id && m.Estado == "Matriculado")
                        .Sum(m => m.Curso != null ? m.Curso.Creditos : 0)
                    : 0,
                cursosMatriculadosActual = periodoActivo != null
                    ? e.Matriculas.Count(m => m.IdPeriodo == periodoActivo.Id && m.Estado == "Matriculado")
                    : 0
            })
            .OrderByDescending(e => e.cicloActual)
            .ThenBy(e => e.codigo)
            .ToListAsync();

        return ServiceOutcome.Ok(estudiantes);
    }

    public async Task<ServiceOutcome> GetEstudianteDetalleAsync(int id)
    {
        var estudiante = await _context.Estudiantes
            .Include(e => e.Usuario)
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Curso!)
                    .ThenInclude(c => c.Docente)
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Periodo)
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Notas)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estudiante == null)
            return ServiceOutcome.NotFound(new { mensaje = "Estudiante no encontrado" });

        var datosPersonales = new
        {
            id = estudiante.Id,
            codigo = estudiante.Codigo,
            nombres = estudiante.Nombres,
            apellidos = estudiante.Apellidos,
            nombreCompleto = $"{estudiante.Nombres} {estudiante.Apellidos}",
            dni = estudiante.Dni,
            email = estudiante.Correo,
            cicloActual = estudiante.CicloActual,
            carrera = estudiante.Carrera,
            estado = estudiante.Estado,
            creditosAcumulados = estudiante.CreditosAcumulados,
            promedioAcumulado = estudiante.PromedioAcumulado,
            promedioSemestral = estudiante.PromedioSemestral,
            fechaIngreso = estudiante.FechaIngreso
        };

        var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);

        var cursosActuales = estudiante.Matriculas
            .Where(m => m.Estado == "Matriculado" &&
                        periodoActivo != null &&
                        m.IdPeriodo == periodoActivo.Id)
            .Select(m => new
            {
                idMatricula = m.Id,
                idCurso = m.IdCurso,
                nombreCurso = m.Curso?.NombreCurso ?? "",
                ciclo = m.Curso?.Ciclo ?? 0,
                creditos = m.Curso?.Creditos ?? 0,
                horasSemanal = m.Curso?.HorasSemanal ?? 0,
                docente = m.Curso?.Docente != null
                    ? $"{m.Curso.Docente.Nombres} {m.Curso.Docente.Apellidos}"
                    : "Sin asignar",
                periodo = m.Periodo?.Nombre ?? "",
                fechaMatricula = m.FechaMatricula,
                estado = m.Estado,
                isAutorizado = m.IsAutorizado,
                notas = m.Notas.Select(n => new
                {
                    tipoEvaluacion = n.TipoEvaluacion,
                    notaValor = n.NotaValor,
                    peso = n.Peso,
                    fecha = n.Fecha
                }).ToList(),
                promedioFinal = m.PromedioFinal ??
                    (m.Notas.Any()
                        ? Math.Round(m.Notas.Sum(n => n.NotaValor * n.Peso / 100), 2)
                        : (decimal?)null)
            })
            .ToList();

        var historialPorPeriodoRaw = estudiante.Matriculas
            .GroupBy(m => new { m.IdPeriodo, m.Periodo?.Nombre, m.Periodo?.Anio, m.Periodo?.Ciclo })
            .Select(g => new
            {
                idPeriodo = g.Key.IdPeriodo,
                nombrePeriodo = g.Key.Nombre ?? "",
                anio = g.Key.Anio ?? 0,
                ciclo = g.Key.Ciclo ?? "",
                esActivo = periodoActivo != null && g.Key.IdPeriodo == periodoActivo.Id,
                totalCursos = g.Count(),
                cursosMatriculados = g.Count(m => m.Estado == "Matriculado"),
                cursosRetirados = g.Count(m => m.Estado == "Retirado"),
                cursosAprobados = g.Count(m =>
                    m.Estado != "Retirado" &&
                    ((m.PromedioFinal.HasValue && m.PromedioFinal.Value >= 10.5m) ||
                    (!m.PromedioFinal.HasValue && m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) >= 10.5m))),
                cursosDesaprobados = g.Count(m =>
                    m.Estado != "Retirado" &&
                    ((m.PromedioFinal.HasValue && m.PromedioFinal.Value < 10.5m) ||
                    (!m.PromedioFinal.HasValue && m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) < 10.5m))),
                creditosMatriculados = g.Where(m => m.Estado == "Matriculado")
                    .Sum(m => m.Curso?.Creditos ?? 0),
                promedioGeneral = g.Where(m => m.Estado != "Retirado" && (m.PromedioFinal.HasValue || m.Notas.Any()))
                    .Select(m => m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0))
                    .DefaultIfEmpty(0)
                    .Average(),
                cursos = g.Select(m => new
                {
                    idMatricula = m.Id,
                    idCurso = m.IdCurso,
                    nombreCurso = m.Curso?.NombreCurso ?? "",
                    ciclo = m.Curso?.Ciclo ?? 0,
                    creditos = m.Curso?.Creditos ?? 0,
                    docente = m.Curso?.Docente != null
                        ? $"{m.Curso.Docente.Nombres} {m.Curso.Docente.Apellidos}"
                        : "Sin asignar",
                    estado = m.Estado,
                    isAutorizado = m.IsAutorizado,
                    promedioFinal = m.PromedioFinal ??
                        (m.Notas.Any()
                            ? Math.Round(m.Notas.Sum(n => n.NotaValor * n.Peso / 100), 2)
                            : (decimal?)null),
                    aprobado = (m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0)) >= 10.5m
                }).ToList()
            })
            .ToList();

        var historialPorPeriodo = historialPorPeriodoRaw
            .OrderByDescending(h => h.anio)
            .ThenByDescending(h => h.ciclo)
            .ToList();

        var totalCursosHistorico = estudiante.Matriculas.Count(m => m.Estado != "Retirado");
        var cursosAprobadosHistorico = estudiante.Matriculas.Count(m =>
            m.Estado != "Retirado" &&
            ((m.PromedioFinal.HasValue && m.PromedioFinal.Value >= 10.5m) ||
             (!m.PromedioFinal.HasValue && m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) >= 10.5m)));

        var cursosDesaprobadosHistorico = estudiante.Matriculas.Count(m =>
            m.Estado != "Retirado" &&
            ((m.PromedioFinal.HasValue && m.PromedioFinal.Value < 10.5m) ||
             (!m.PromedioFinal.HasValue && m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) < 10.5m)));

        var creditosTotales = estudiante.Matriculas
            .Where(m => m.Estado != "Retirado")
            .Sum(m => m.Curso?.Creditos ?? 0);

        var creditosAprobados = estudiante.Matriculas
            .Where(m => m.Estado != "Retirado" &&
                        ((m.PromedioFinal.HasValue && m.PromedioFinal.Value >= 10.5m) ||
                         (!m.PromedioFinal.HasValue && m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) >= 10.5m)))
            .Sum(m => m.Curso?.Creditos ?? 0);

        var estadisticas = new
        {
            totalCursosHistorico,
            cursosAprobadosHistorico,
            cursosDesaprobadosHistorico,
            creditosTotales,
            creditosAprobados,
            creditosPendientes = Math.Max(0, creditosTotales - creditosAprobados),
            porcentajeAprobacion = totalCursosHistorico > 0 ? (decimal)cursosAprobadosHistorico / totalCursosHistorico * 100 : 0,
            promedioGeneralHistorico = estudiante.Matriculas
                .Where(m => m.Estado != "Retirado" && (m.PromedioFinal.HasValue || m.Notas.Any()))
                .Select(m => m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0))
                .DefaultIfEmpty(0)
                .Average()
        };

        return ServiceOutcome.Ok(new
        {
            datosPersonales,
            cursosActuales,
            historialPorPeriodo,
            estadisticas
        });
    }

    public async Task<ServiceOutcome> CrearEstudianteAsync(CrearEstudianteDto dto)
    {
        var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (usuarioExiste)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "El email ya está registrado" });
        }

        var dniExiste = await _context.Estudiantes.AnyAsync(e => e.Dni == dto.NumeroDocumento);
        if (dniExiste)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "El DNI ya está registrado" });
        }

        var nuevoUsuario = new Usuario
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 10),
            Rol = "Estudiante",
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now,
            Estado = true
        };

        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        var codigoEstudiante = $"EST{DateTime.Now:yyyyMMdd}{nuevoUsuario.Id:D4}";

        var nuevoEstudiante = new Estudiante
        {
            IdUsuario = nuevoUsuario.Id,
            Codigo = codigoEstudiante,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            Dni = dto.NumeroDocumento,
            Correo = dto.Email,
            CicloActual = dto.Ciclo,
            PromedioAcumulado = 0,
            PromedioSemestral = 0,
            CreditosAcumulados = 0,
            FechaIngreso = DateTime.Now,
            Estado = "Activo",
            Carrera = "Ingeniería de Sistemas"
        };

        _context.Estudiantes.Add(nuevoEstudiante);
        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Estudiante creado exitosamente",
            estudiante = new
            {
                id = nuevoEstudiante.Id,
                codigo = nuevoEstudiante.Codigo,
                email = nuevoUsuario.Email,
                nombres = nuevoUsuario.Nombres,
                apellidos = nuevoUsuario.Apellidos,
                ciclo = nuevoEstudiante.CicloActual
            }
        });
    }

    public async Task<ServiceOutcome> EliminarEstudianteAsync(int id)
    {
        var estudiante = await _context.Estudiantes
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Notas)
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estudiante == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Estudiante no encontrado" });
        }

        int totalMatriculas = estudiante.Matriculas?.Count ?? 0;
        int totalNotas = estudiante.Matriculas?.Sum(m => m.Notas.Count) ?? 0;

        if (estudiante.Matriculas != null)
        {
            foreach (var matricula in estudiante.Matriculas)
            {
                if (matricula.Notas != null && matricula.Notas.Any())
                {
                    _context.Notas.RemoveRange(matricula.Notas);
                }
            }

            _context.Matriculas.RemoveRange(estudiante.Matriculas);
        }

        var asistencias = await _context.Asistencias
            .Where(a => a.IdEstudiante == id)
            .ToListAsync();

        if (asistencias.Any())
        {
            _context.Asistencias.RemoveRange(asistencias);
        }

        var usuario = estudiante.Usuario;

        _context.Estudiantes.Remove(estudiante);
        if (usuario != null)
        {
            _context.Usuarios.Remove(usuario);
        }

        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Estudiante eliminado exitosamente",
            eliminado = new
            {
                id = estudiante.Id,
                codigo = estudiante.Codigo,
                nombreCompleto = $"{estudiante.Nombres} {estudiante.Apellidos}",
                email = estudiante.Correo,
                matriculasEliminadas = totalMatriculas,
                notasEliminadas = totalNotas,
                asistenciasEliminadas = asistencias.Count
            }
        });
    }

    public async Task<ServiceOutcome> CrearCursosDirigidosAsync(MatriculaDirigidaDto dto)
    {
        var periodo = await _context.Periodos.FindAsync(dto.IdPeriodo);
        if (periodo == null)
            return ServiceOutcome.BadRequest(new { mensaje = "El período no existe" });

        var curso = await _context.Cursos
            .Include(c => c.Docente)
            .FirstOrDefaultAsync(c => c.Id == dto.IdCurso);

        if (curso == null)
            return ServiceOutcome.BadRequest(new { mensaje = "El curso no existe" });

        int exitosos = 0;
        int fallidos = 0;
        var listaExitosos = new List<object>();
        var listaErrores = new List<object>();

        foreach (var idEstudiante in dto.IdsEstudiantes)
        {
            try
            {
                var estudiante = await _context.Estudiantes
                    .Include(e => e.Usuario)
                    .FirstOrDefaultAsync(e => e.Id == idEstudiante);

                if (estudiante == null)
                {
                    fallidos++;
                    listaErrores.Add(new { idEstudiante, error = "Estudiante no encontrado" });
                    continue;
                }

                var matriculaDto = new MatricularDto
                {
                    IdCurso = dto.IdCurso,
                    IdPeriodo = dto.IdPeriodo
                };

                _ = await _estudianteService.MatricularAsync(idEstudiante, matriculaDto, isAutorizado: true);

                exitosos++;
                listaExitosos.Add(new
                {
                    idEstudiante,
                    nombreEstudiante = $"{estudiante.Nombres} {estudiante.Apellidos}",
                    curso = curso.NombreCurso,
                    periodo = periodo.Nombre,
                    estado = "Matriculado (Autorizado)"
                });
            }
            catch (Exception ex)
            {
                fallidos++;
                listaErrores.Add(new { idEstudiante, error = ex.Message });
            }
        }

        return ServiceOutcome.Ok(new
        {
            mensaje = $"Proceso completado: {exitosos} exitosos, {fallidos} fallidos",
            exitosos,
            fallidos,
            detalles = new
            {
                exitosos = listaExitosos,
                errores = listaErrores
            }
        });
    }

    public async Task<ServiceOutcome> GetPeriodosAsync()
    {
        var periodos = await _context.Periodos
            .OrderByDescending(p => p.Anio)
            .ThenByDescending(p => p.Ciclo)
            .Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                anio = p.Anio,
                ciclo = p.Ciclo,
                fechaInicio = p.FechaInicio,
                fechaFin = p.FechaFin,
                activo = p.Activo,
                fechaCreacion = p.FechaCreacion,
                totalMatriculas = p.Matriculas.Count
            })
            .ToListAsync();

        return ServiceOutcome.Ok(periodos);
    }

    public async Task<ServiceOutcome> CrearPeriodoAsync(CrearPeriodoDto dto)
    {
        var periodoExiste = await _context.Periodos
            .AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.ToLower());

        if (periodoExiste)
            return ServiceOutcome.BadRequest(new { mensaje = "Ya existe un período con ese nombre" });

        if (dto.FechaInicio >= dto.FechaFin)
            return ServiceOutcome.BadRequest(new { mensaje = "La fecha de inicio debe ser anterior a la fecha de fin" });

        var nuevoPeriodo = new Periodo
        {
            Nombre = dto.Nombre,
            Anio = dto.Anio,
            Ciclo = dto.Ciclo,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            Activo = false,
            FechaCreacion = DateTime.Now
        };

        _context.Periodos.Add(nuevoPeriodo);
        await _context.SaveChangesAsync();

        if (dto.Activo)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.sp_AbrirPeriodo @IdPeriodoNuevo";
                command.CommandType = System.Data.CommandType.Text;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@IdPeriodoNuevo";
                parameter.Value = nuevoPeriodo.Id;
                command.Parameters.Add(parameter);

                await _context.Database.OpenConnectionAsync();

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }

            await _context.Entry(nuevoPeriodo).ReloadAsync();
        }

        return ServiceOutcome.Ok(new
        {
            mensaje = dto.Activo
                ? "Período creado y activado exitosamente. Los estudiantes con evaluaciones del periodo anterior avanzaron de ciclo."
                : "Período creado exitosamente",
            periodo = new
            {
                id = nuevoPeriodo.Id,
                nombre = nuevoPeriodo.Nombre,
                anio = nuevoPeriodo.Anio,
                ciclo = nuevoPeriodo.Ciclo,
                fechaInicio = nuevoPeriodo.FechaInicio,
                fechaFin = nuevoPeriodo.FechaFin,
                activo = nuevoPeriodo.Activo
            }
        });
    }

    public async Task<ServiceOutcome> EditarPeriodoAsync(int id, EditarPeriodoDto dto)
    {
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo == null)
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });

        var nombreDuplicado = await _context.Periodos
            .AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.ToLower() && p.Id != id);

        if (nombreDuplicado)
            return ServiceOutcome.BadRequest(new { mensaje = "Ya existe otro período con ese nombre" });

        if (dto.FechaInicio >= dto.FechaFin)
            return ServiceOutcome.BadRequest(new { mensaje = "La fecha de inicio debe ser anterior a la fecha de fin" });

        periodo.Nombre = dto.Nombre;
        periodo.Anio = dto.Anio;
        periodo.Ciclo = dto.Ciclo;
        periodo.FechaInicio = dto.FechaInicio;
        periodo.FechaFin = dto.FechaFin;

        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Período actualizado exitosamente",
            periodo = new
            {
                id = periodo.Id,
                nombre = periodo.Nombre,
                anio = periodo.Anio,
                ciclo = periodo.Ciclo,
                fechaInicio = periodo.FechaInicio,
                fechaFin = periodo.FechaFin,
                activo = periodo.Activo
            }
        });
    }

    public async Task<ServiceOutcome> ActivarPeriodoAsync(int id)
    {
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo == null)
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });

        var periodoAnterior = await _context.Periodos
            .Where(p => p.Activo)
            .FirstOrDefaultAsync();

        var todosLosPeriodos = await _context.Periodos.ToListAsync();
        foreach (var p in todosLosPeriodos)
        {
            p.Activo = false;
        }

        periodo.Activo = true;

        int estudiantesAvanzaron = 0;
        if (periodoAnterior != null)
        {
            var estudiantes = await _context.Estudiantes
                .Where(e => e.Estado == "Activo")
                .ToListAsync();

            foreach (var estudiante in estudiantes)
            {
                var matriculasPeriodoAnterior = await _context.Matriculas
                    .Where(m => m.IdEstudiante == estudiante.Id &&
                               m.IdPeriodo == periodoAnterior.Id &&
                               m.Estado != "Retirado" &&
                               m.PromedioFinal.HasValue)
                    .ToListAsync();

                if (matriculasPeriodoAnterior.Any())
                {
                    if (estudiante.CicloActual < 10)
                    {
                        estudiante.CicloActual += 1;
                        estudiantesAvanzaron++;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = $"Período {periodo.Nombre} activado exitosamente",
            estudiantesAvanzaronCiclo = estudiantesAvanzaron,
            periodo = new
            {
                id = periodo.Id,
                nombre = periodo.Nombre,
                activo = periodo.Activo
            }
        });
    }

    public async Task<ServiceOutcome> EliminarPeriodoAsync(int id)
    {
        var periodo = await _context.Periodos
            .Include(p => p.Matriculas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (periodo == null)
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });

        if (periodo.Matriculas.Any())
        {
            return ServiceOutcome.BadRequest(new
            {
                mensaje = "No se puede eliminar el período porque tiene matrículas asociadas",
                totalMatriculas = periodo.Matriculas.Count
            });
        }

        _context.Periodos.Remove(periodo);
        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new { mensaje = "Período eliminado exitosamente" });
    }

    public async Task<ServiceOutcome> GetTodosDocentesAsync()
    {
        var docentes = await _context.Docentes
            .Include(d => d.Cursos)
            .OrderBy(d => d.FechaCreacion)
            .Select(d => new
            {
                id = d.Id,
                apellidos = d.Apellidos,
                nombres = d.Nombres,
                nombreCompleto = d.Apellidos + ", " + d.Nombres,
                profesion = d.Profesion,
                fechaNacimiento = d.FechaNacimiento,
                correo = d.Correo,
                tienePassword = !string.IsNullOrEmpty(d.PasswordHash),
                totalCursos = d.Cursos.Count,
                fechaCreacion = d.FechaCreacion
            })
            .ToListAsync();

        return ServiceOutcome.Ok(docentes);
    }

    public async Task<ServiceOutcome> CrearDocenteAsync(CrearDocenteConPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
        }

        if (!string.IsNullOrEmpty(dto.Correo))
        {
            var existeCorreo = await _context.Docentes.AnyAsync(d => d.Correo == dto.Correo);
            if (existeCorreo)
            {
                return ServiceOutcome.BadRequest(new { mensaje = "Ya existe un docente con este correo electrónico" });
            }
        }

        var docente = new Docente
        {
            Apellidos = dto.Apellidos,
            Nombres = dto.Nombres,
            Profesion = dto.Profesion,
            FechaNacimiento = dto.FechaNacimiento,
            Correo = dto.Correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(11))
        };

        _context.Docentes.Add(docente);
        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Docente creado exitosamente",
            docente = new
            {
                id = docente.Id,
                apellidos = docente.Apellidos,
                nombres = docente.Nombres,
                nombreCompleto = docente.Apellidos + ", " + docente.Nombres,
                profesion = docente.Profesion,
                correo = docente.Correo,
                tienePassword = true
            }
        });
    }

    public async Task<ServiceOutcome> AsignarPasswordDocenteAsync(int id, AsignarPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
        }

        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Docente no encontrado" });
        }

        docente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(11));
        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Contraseña asignada exitosamente",
            docente = new
            {
                id = docente.Id,
                nombreCompleto = docente.Apellidos + ", " + docente.Nombres,
                tienePassword = true
            }
        });
    }

    public async Task<ServiceOutcome> ActualizarDocenteAsync(int id, ActualizarDocenteDto dto)
    {
        var docente = await _context.Docentes.FindAsync(id);
        if (docente == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Docente no encontrado" });
        }

        if (!string.IsNullOrEmpty(dto.Correo))
        {
            var existeCorreo = await _context.Docentes
                .AnyAsync(d => d.Correo == dto.Correo && d.Id != id);

            if (existeCorreo)
            {
                return ServiceOutcome.BadRequest(new { mensaje = "Ya existe otro docente con este correo electrónico" });
            }
        }

        docente.Apellidos = dto.Apellidos;
        docente.Nombres = dto.Nombres;
        docente.Profesion = dto.Profesion;
        docente.FechaNacimiento = dto.FechaNacimiento;
        docente.Correo = dto.Correo;

        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new
        {
            mensaje = "Docente actualizado exitosamente",
            docente = new
            {
                id = docente.Id,
                apellidos = docente.Apellidos,
                nombres = docente.Nombres,
                nombreCompleto = docente.Apellidos + ", " + docente.Nombres,
                profesion = docente.Profesion,
                correo = docente.Correo,
                tienePassword = !string.IsNullOrEmpty(docente.PasswordHash)
            }
        });
    }

    public async Task<ServiceOutcome> EliminarDocenteAsync(int id)
    {
        var docente = await _context.Docentes
            .Include(d => d.Cursos)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (docente == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Docente no encontrado" });
        }

        int cursosDesasignados = docente.Cursos?.Count ?? 0;

        if (docente.Cursos != null && docente.Cursos.Any())
        {
            foreach (var curso in docente.Cursos)
            {
                curso.IdDocente = null;
            }
        }

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync();

        var mensaje = cursosDesasignados > 0
            ? $"Docente eliminado exitosamente. Se desasignaron {cursosDesasignados} curso(s)."
            : "Docente eliminado exitosamente";

        return ServiceOutcome.Ok(new
        {
            mensaje,
            cursosDesasignados,
            docente = new
            {
                id = docente.Id,
                nombreCompleto = docente.Apellidos + ", " + docente.Nombres
            }
        });
    }

    public async Task<ServiceOutcome> CerrarPeriodoAsync(int id)
    {
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });
        }

        if (!periodo.Activo)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "El período ya está cerrado" });
        }

        var estudiantesConMatriculas = await _context.Matriculas
            .Where(m => m.IdPeriodo == id && m.Estado == "Matriculado")
            .Select(m => m.IdEstudiante)
            .Distinct()
            .CountAsync();

        int totalEstudiantes = 0;
        int totalMatriculas = 0;
        int cursosAprobados = 0;
        int cursosDesaprobados = 0;

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "EXEC dbo.sp_CerrarPeriodo @IdPeriodo";
            command.CommandType = System.Data.CommandType.Text;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@IdPeriodo";
            parameter.Value = id;
            command.Parameters.Add(parameter);

            await _context.Database.OpenConnectionAsync();

            try
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        totalEstudiantes = reader.GetInt32(0);
                        totalMatriculas = reader.GetInt32(1);
                        cursosAprobados = reader.GetInt32(2);
                        cursosDesaprobados = reader.GetInt32(3);
                    }
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        var estudiantesPromovidos = 0;
        var estudiantesRetenidos = 0;

        var estudiantesConResultados = await _context.Matriculas
            .Where(m => m.IdPeriodo == id && m.Estado == "Matriculado" && m.PromedioFinal.HasValue)
            .GroupBy(m => m.IdEstudiante)
            .Select(g => new
            {
                IdEstudiante = g.Key,
                TotalCursos = g.Count(),
                CursosAprobados = g.Count(m => m.PromedioFinal >= 11),
                CursosReprobados = g.Count(m => m.PromedioFinal < 11)
            })
            .ToListAsync();

        foreach (var est in estudiantesConResultados)
        {
            if (est.CursosReprobados == 0 || est.CursosAprobados > est.CursosReprobados)
            {
                estudiantesPromovidos++;
            }
            else
            {
                estudiantesRetenidos++;
            }
        }

        var totalProcesados = totalEstudiantes > 0 ? totalEstudiantes : estudiantesConMatriculas;

        return ServiceOutcome.Ok(new
        {
            mensaje = "Período cerrado exitosamente",
            estudiantesPromovidos,
            estudiantesRetenidos,
            totalEstudiantesProcesados = totalProcesados,
            estadisticas = new
            {
                totalEstudiantes = totalProcesados,
                totalMatriculas,
                cursosAprobados,
                cursosDesaprobados,
                fechaCierre = DateTime.Now.ToString("o")
            }
        });
    }

    public async Task<ServiceOutcome> AbrirPeriodoAsync(int id)
    {
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });
        }

        if (periodo.Activo)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "El período ya está activo" });
        }

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "EXEC dbo.sp_AbrirPeriodo @IdPeriodoNuevo";
            command.CommandType = System.Data.CommandType.Text;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@IdPeriodoNuevo";
            parameter.Value = id;
            command.Parameters.Add(parameter);

            await _context.Database.OpenConnectionAsync();

            try
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var resumenCiclos = new List<object>();

                    if (reader.Read())
                    {
                        do
                        {
                            var ciclo = reader.GetInt32(0);
                            var cantidad = reader.GetInt32(1);

                            resumenCiclos.Add(new
                            {
                                ciclo,
                                cantidadEstudiantes = cantidad
                            });
                        } while (reader.Read());
                    }

                    return ServiceOutcome.Ok(new
                    {
                        mensaje = "Período abierto exitosamente. Los estudiantes con notas del periodo anterior avanzaron de ciclo.",
                        periodoActivo = new
                        {
                            id = periodo.Id,
                            nombre = periodo.Nombre,
                            anio = periodo.Anio,
                            ciclo = periodo.Ciclo
                        },
                        resumenCiclos,
                        fechaApertura = DateTime.Now.ToString("o")
                    });
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task<ServiceOutcome> ValidarCierrePeriodoAsync(int id)
    {
        var periodo = await _context.Periodos.FindAsync(id);
        if (periodo == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Período no encontrado" });
        }

        if (!periodo.Activo)
        {
            return ServiceOutcome.BadRequest(new { mensaje = "El período ya está cerrado" });
        }

        var matriculas = await _context.Matriculas
            .Include(m => m.Estudiante)
            .Include(m => m.Curso)
            .Include(m => m.Notas)
            .Where(m => m.IdPeriodo == id && m.Estado == "Matriculado")
            .ToListAsync();

        var estudiantesSinNotasCompletas = new List<object>();
        var advertencias = new List<string>();
        int conNotasCompletas = 0;
        int aprobadosEstimados = 0;
        int desaprobadosEstimados = 0;

        foreach (var matricula in matriculas)
        {
            var tiposEvaluacion = await _context.TiposEvaluacion
                .Where(t => t.IdCurso == matricula.IdCurso && t.Activo)
                .ToListAsync();

            if (!tiposEvaluacion.Any())
            {
                advertencias.Add($"El curso '{matricula.Curso?.NombreCurso}' no tiene tipos de evaluación configurados");
                continue;
            }

            var cursosPendientes = new List<object>();

            foreach (var tipo in tiposEvaluacion)
            {
                var notaExiste = matricula.Notas.Any(n =>
                    n.TipoEvaluacion.Equals(tipo.Nombre, StringComparison.OrdinalIgnoreCase));
                if (!notaExiste)
                {
                    cursosPendientes.Add(new
                    {
                        idCurso = matricula.Curso?.Id ?? 0,
                        nombreCurso = matricula.Curso?.NombreCurso ?? "",
                        razon = $"Falta nota de {tipo.Nombre}"
                    });
                }
            }

            if (cursosPendientes.Any())
            {
                estudiantesSinNotasCompletas.Add(new
                {
                    idEstudiante = matricula.Estudiante?.Id ?? 0,
                    nombreEstudiante = $"{matricula.Estudiante?.Nombres} {matricula.Estudiante?.Apellidos}",
                    codigo = matricula.Estudiante?.Codigo ?? "",
                    cursosPendientes
                });
            }
            else
            {
                conNotasCompletas++;

                decimal promedioFinal = 0;
                foreach (var nota in matricula.Notas)
                {
                    promedioFinal += nota.NotaValor * (nota.Peso / 100m);
                }

                int promedioRedondeado = (int)Math.Round(promedioFinal, 0, MidpointRounding.AwayFromZero);

                if (promedioRedondeado >= 11)
                    aprobadosEstimados++;
                else
                    desaprobadosEstimados++;
            }
        }

        var puedeSerCerrado = !estudiantesSinNotasCompletas.Any();

        if (!puedeSerCerrado)
        {
            advertencias.Add($"Hay {estudiantesSinNotasCompletas.Count} estudiante(s) con notas incompletas");
        }

        _ = aprobadosEstimados;
        _ = desaprobadosEstimados;

        return ServiceOutcome.Ok(new
        {
            puedeSerCerrado,
            advertencias,
            totalMatriculas = matriculas.Count,
            matriculasCompletas = conNotasCompletas,
            matriculasIncompletas = estudiantesSinNotasCompletas.Count,
            estudiantesSinNotasCompletas
        });
    }
}


