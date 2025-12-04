using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

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
        private readonly IEstudianteService _estudianteService;
        private readonly GestionAcademicaContext _context;

        public AdminController(IEstudianteService estudianteService, GestionAcademicaContext context)
        {
            _estudianteService = estudianteService;
            _context = context;
        }

        /// <summary>
        /// Verifica que el usuario actual sea administrador
        /// </summary>
        private bool EsAdministrador()
        {
            var rolClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return rolClaim == "Administrador";
        }

        /// <summary>
        /// Obtiene todos los estudiantes registrados en el sistema con información consolidada
        /// Incluye datos completos: DNI, email, créditos y cursos actuales
        /// Solo accesible para administradores
        /// </summary>
        [HttpGet("estudiantes")]
        public async Task<ActionResult> GetTodosEstudiantes()
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Obtener período activo
                var periodoActivo = await _context.Periodos
                    .FirstOrDefaultAsync(p => p.Activo);

                // Obtener todos los estudiantes con sus datos básicos y relaciones necesarias
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
                        // Calcular créditos del semestre actual (solo cursos matriculados, NO retirados)
                        creditosSemestreActual = periodoActivo != null
                            ? e.Matriculas
                                .Where(m => m.IdPeriodo == periodoActivo.Id && m.Estado == "Matriculado")
                                .Sum(m => m.Curso != null ? m.Curso.Creditos : 0)
                            : 0,
                        // Contar cursos matriculados actualmente (NO retirados)
                        cursosMatriculadosActual = periodoActivo != null
                            ? e.Matriculas
                                .Count(m => m.IdPeriodo == periodoActivo.Id && m.Estado == "Matriculado")
                            : 0
                    })
                    .OrderByDescending(e => e.cicloActual)
                    .ThenBy(e => e.codigo)
                    .ToListAsync();

                return Ok(estudiantes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener estudiantes", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene información detallada de un estudiante específico
        /// Incluye: datos personales, cursos actuales, historial completo, estadísticas
        /// Solo accesible para administradores
        /// </summary>
        [HttpGet("estudiantes/{id}/detalle")]
        public async Task<ActionResult> GetEstudianteDetalle(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Obtener estudiante con todas sus relaciones
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
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                // Datos personales
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

                // Obtener período activo
                var periodoActivo = await _context.Periodos
                    .FirstOrDefaultAsync(p => p.Activo);

                // Cursos actuales (del período activo)
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
                        isAutorizado = m.IsAutorizado, // Indica si es curso dirigido
                        notas = m.Notas.Select(n => new
                        {
                            tipoEvaluacion = n.TipoEvaluacion,
                            notaValor = n.NotaValor,
                            peso = n.Peso,
                            fecha = n.Fecha
                        }).ToList(),
                        // Usar PromedioFinal guardado si existe (período cerrado), sino calcular
                        promedioFinal = m.PromedioFinal ?? 
                            (m.Notas.Any() 
                                ? Math.Round(m.Notas.Sum(n => n.NotaValor * n.Peso / 100), 2)
                                : (decimal?)null)
                    })
                    .ToList();

                // Historial completo por períodos
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
                        // IMPORTANTE: Solo contar aprobados/desaprobados de cursos NO retirados
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
                        // IMPORTANTE: Promedio solo de cursos NO retirados
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
                            fechaMatricula = m.FechaMatricula,
                            fechaRetiro = m.FechaRetiro,
                            // Usar PromedioFinal guardado si existe, sino calcular
                            promedioFinal = m.PromedioFinal ?? 
                                (m.Notas.Any() 
                                    ? Math.Round(m.Notas.Sum(n => n.NotaValor * n.Peso / 100), 2)
                                    : (decimal?)null),
                            aprobado = m.PromedioFinal.HasValue 
                                ? m.PromedioFinal.Value >= 10.5m
                                : (m.Notas.Any() && m.Notas.Sum(n => n.NotaValor * n.Peso / 100) >= 10.5m)
                        }).ToList()
                    })
                    .OrderBy(g => g.anio)  // Ordenar cronológicamente
                    .ThenBy(g => g.ciclo)
                    .ToList();

                // Agregar cicloAcademico (1, 2, 3... secuencial)
                var historialPorPeriodo = historialPorPeriodoRaw
                    .Select((g, index) => new
                    {
                        g.idPeriodo,
                        g.nombrePeriodo,
                        g.anio,
                        g.ciclo,
                        cicloAcademico = index + 1,  // Ciclo académico secuencial (1, 2, 3, 4...)
                        g.esActivo,
                        g.totalCursos,
                        g.cursosMatriculados,
                        g.cursosRetirados,
                        g.cursosAprobados,
                        g.cursosDesaprobados,
                        g.creditosMatriculados,
                        g.promedioGeneral,
                        g.cursos
                    })
                    .OrderByDescending(g => g.anio)  // Ordenar para visualización: más reciente primero
                    .ThenByDescending(g => g.ciclo)
                    .ToList();

                // Estadísticas generales
                var totalMatriculas = estudiante.Matriculas.Count;
                var totalCursosActivos = estudiante.Matriculas.Count(m => m.Estado == "Matriculado");
                var totalCursosRetirados = estudiante.Matriculas.Count(m => m.Estado == "Retirado");
                var totalCursosDirigidos = estudiante.Matriculas.Count(m => m.IsAutorizado);
                
                // IMPORTANTE: Cursos con notas EXCLUYENDO los retirados
                var cursosConNotas = estudiante.Matriculas
                    .Where(m => m.Estado != "Retirado" && (m.PromedioFinal.HasValue || m.Notas.Any()))
                    .ToList();
                
                var totalCursosAprobados = cursosConNotas
                    .Count(m => (m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0)) >= 10.5m);
                
                var totalCursosDesaprobados = cursosConNotas
                    .Count(m => (m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0)) < 10.5m);

                var promedioGeneralHistorico = cursosConNotas.Any()
                    ? cursosConNotas.Average(m => m.PromedioFinal ?? (m.Notas.Any() ? m.Notas.Sum(n => n.NotaValor * n.Peso / 100) : 0))
                    : 0;

                var estadisticas = new
                {
                    totalMatriculas,
                    totalCursosActivos,
                    totalCursosRetirados,
                    totalCursosDirigidos,
                    totalCursosAprobados,
                    totalCursosDesaprobados,
                    promedioGeneralHistorico = Math.Round(promedioGeneralHistorico, 2),
                    creditosAcumulados = estudiante.CreditosAcumulados,
                    promedioAcumulado = estudiante.PromedioAcumulado,
                    promedioSemestral = estudiante.PromedioSemestral
                };

                return Ok(new
                {
                    datosPersonales,
                    cursosActuales,
                    historialPorPeriodo,
                    estadisticas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener detalle del estudiante", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo estudiante con su usuario asociado
        /// Solo accesible para administradores
        /// </summary>
        [HttpPost("estudiantes")]
        public async Task<ActionResult> CrearEstudiante([FromBody] CrearEstudianteDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Validar que el email no exista
                var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
                if (usuarioExiste)
                {
                    return BadRequest(new { mensaje = "El email ya está registrado" });
                }

                // Validar que el DNI no exista
                var dniExiste = await _context.Estudiantes.AnyAsync(e => e.Dni == dto.NumeroDocumento);
                if (dniExiste)
                {
                    return BadRequest(new { mensaje = "El DNI ya está registrado" });
                }

                // Crear usuario
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

                // Generar código único para el estudiante
                var codigoEstudiante = $"EST{DateTime.Now:yyyyMMdd}{nuevoUsuario.Id:D4}";

                // Crear registro de estudiante
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

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear estudiante", detalle = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/admin/estudiantes/{id} - Elimina un estudiante del sistema
        /// Elimina también sus matrículas, notas y el usuario asociado
        /// </summary>
        [HttpDelete("estudiantes/{id}")]
        public async Task<ActionResult> EliminarEstudiante(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Buscar el estudiante con sus relaciones
                var estudiante = await _context.Estudiantes
                    .Include(e => e.Matriculas)
                        .ThenInclude(m => m.Notas)
                    .Include(e => e.Usuario)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (estudiante == null)
                {
                    return NotFound(new { mensaje = "Estudiante no encontrado" });
                }

                // Contar matrículas y notas que se eliminarán
                int totalMatriculas = estudiante.Matriculas?.Count ?? 0;
                int totalNotas = estudiante.Matriculas?.Sum(m => m.Notas.Count) ?? 0;

                // Eliminar todas las notas del estudiante
                if (estudiante.Matriculas != null)
                {
                    foreach (var matricula in estudiante.Matriculas)
                    {
                        if (matricula.Notas != null && matricula.Notas.Any())
                        {
                            _context.Notas.RemoveRange(matricula.Notas);
                        }
                    }

                    // Eliminar todas las matrículas del estudiante
                    _context.Matriculas.RemoveRange(estudiante.Matriculas);
                }

                // Eliminar asistencias del estudiante
                var asistencias = await _context.Asistencias
                    .Where(a => a.IdEstudiante == id)
                    .ToListAsync();
                
                if (asistencias.Any())
                {
                    _context.Asistencias.RemoveRange(asistencias);
                }

                // Eliminar el usuario asociado si existe
                var usuario = estudiante.Usuario;
                
                // Eliminar el estudiante
                _context.Estudiantes.Remove(estudiante);
                
                // Eliminar usuario después del estudiante
                if (usuario != null)
                {
                    _context.Usuarios.Remove(usuario);
                }

                await _context.SaveChangesAsync();

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar estudiante", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crea matrículas dirigidas (autorizadas) que omiten restricciones de ciclo
        /// Solo accesible para administradores
        /// </summary>
        [HttpPost("cursos-dirigidos")]
        public async Task<ActionResult> CrearCursosDirigidos([FromBody] MatriculaDirigidaDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Validar que el período existe
                var periodo = await _context.Periodos.FindAsync(dto.IdPeriodo);
                if (periodo == null)
                    return BadRequest(new { mensaje = "El período no existe" });

                // Validar que el curso existe
                var curso = await _context.Cursos
                    .Include(c => c.Docente)
                    .FirstOrDefaultAsync(c => c.Id == dto.IdCurso);
                
                if (curso == null)
                    return BadRequest(new { mensaje = "El curso no existe" });

                var resultados = new
                {
                    exitosos = 0,
                    fallidos = 0,
                    detalles = new
                    {
                        exitosos = new List<object>(),
                        errores = new List<object>()
                    }
                };

                int exitosos = 0;
                int fallidos = 0;
                var listaExitosos = new List<object>();
                var listaErrores = new List<object>();

                // Procesar cada estudiante
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
                            listaErrores.Add(new
                            {
                                idEstudiante,
                                error = "Estudiante no encontrado"
                            });
                            continue;
                        }

                        // Crear la matrícula usando el servicio con isAutorizado = true
                        var matriculaDto = new MatricularDto
                        {
                            IdCurso = dto.IdCurso,
                            IdPeriodo = dto.IdPeriodo
                        };

                        var matricula = await _estudianteService.MatricularAsync(idEstudiante, matriculaDto, isAutorizado: true);

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
                        listaErrores.Add(new
                        {
                            idEstudiante,
                            error = ex.Message
                        });
                    }
                }

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear cursos dirigidos", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene todos los períodos académicos
        /// Solo accesible para administradores
        /// </summary>
        [HttpGet("periodos")]
        public async Task<ActionResult> GetPeriodos()
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

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

                return Ok(periodos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener períodos", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo período académico
        /// Solo accesible para administradores
        /// </summary>
        [HttpPost("periodos")]
        public async Task<ActionResult> CrearPeriodo([FromBody] CrearPeriodoDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Validar que no exista un período con el mismo nombre
                var periodoExiste = await _context.Periodos
                    .AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.ToLower());

                if (periodoExiste)
                    return BadRequest(new { mensaje = "Ya existe un período con ese nombre" });

                // Validar fechas
                if (dto.FechaInicio >= dto.FechaFin)
                    return BadRequest(new { mensaje = "La fecha de inicio debe ser anterior a la fecha de fin" });

                // Crear el nuevo período (siempre inactivo al inicio)
                var nuevoPeriodo = new Periodo
                {
                    Nombre = dto.Nombre,
                    Anio = dto.Anio,
                    Ciclo = dto.Ciclo,
                    FechaInicio = dto.FechaInicio,
                    FechaFin = dto.FechaFin,
                    Activo = false, // Siempre crear como inactivo primero
                    FechaCreacion = DateTime.Now
                };

                _context.Periodos.Add(nuevoPeriodo);
                await _context.SaveChangesAsync();

                // Si se debe activar, usar el SP sp_AbrirPeriodo
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

                    // Recargar el período para obtener el estado actualizado
                    await _context.Entry(nuevoPeriodo).ReloadAsync();
                }

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear período", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un período académico existente
        /// Solo accesible para administradores
        /// </summary>
        [HttpPut("periodos/{id}")]
        public async Task<ActionResult> EditarPeriodo(int id, [FromBody] EditarPeriodoDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos.FindAsync(id);
                if (periodo == null)
                    return NotFound(new { mensaje = "Período no encontrado" });

                // Validar que no exista otro período con el mismo nombre
                var nombreDuplicado = await _context.Periodos
                    .AnyAsync(p => p.Nombre.ToLower() == dto.Nombre.ToLower() && p.Id != id);

                if (nombreDuplicado)
                    return BadRequest(new { mensaje = "Ya existe otro período con ese nombre" });

                // Validar fechas
                if (dto.FechaInicio >= dto.FechaFin)
                    return BadRequest(new { mensaje = "La fecha de inicio debe ser anterior a la fecha de fin" });

                periodo.Nombre = dto.Nombre;
                periodo.Anio = dto.Anio;
                periodo.Ciclo = dto.Ciclo;
                periodo.FechaInicio = dto.FechaInicio;
                periodo.FechaFin = dto.FechaFin;

                await _context.SaveChangesAsync();

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar período", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Activa un período académico y desactiva los demás
        /// Solo accesible para administradores
        /// </summary>
        [HttpPut("periodos/{id}/activar")]
        public async Task<ActionResult> ActivarPeriodo(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos.FindAsync(id);
                if (periodo == null)
                    return NotFound(new { mensaje = "Período no encontrado" });

                // Obtener el período anterior (el que estaba activo)
                var periodoAnterior = await _context.Periodos
                    .Where(p => p.Activo)
                    .FirstOrDefaultAsync();

                // Desactivar todos los períodos
                var todosLosPeriodos = await _context.Periodos.ToListAsync();
                foreach (var p in todosLosPeriodos)
                {
                    p.Activo = false;
                }

                // Activar el período seleccionado
                periodo.Activo = true;

                // ✅ AVANZAR CICLO DE ESTUDIANTES que cursaron el período anterior
                // El ciclo avanza por cada período cursado (no importa si aprobó o no)
                int estudiantesAvanzaron = 0;
                
                if (periodoAnterior != null)
                {
                    // Obtener todos los estudiantes activos
                    var estudiantes = await _context.Estudiantes
                        .Where(e => e.Estado == "Activo")
                        .ToListAsync();
                    
                    foreach (var estudiante in estudiantes)
                    {
                        // Obtener matrículas del período anterior (excluir retirados)
                        var matriculasPeriodoAnterior = await _context.Matriculas
                            .Where(m => m.IdEstudiante == estudiante.Id && 
                                       m.IdPeriodo == periodoAnterior.Id &&
                                       m.Estado != "Retirado" &&
                                       m.PromedioFinal.HasValue)  // Solo si tiene nota final (período cerrado)
                            .ToListAsync();
                        
                        // Si tuvo matrículas con notas en el período anterior, avanza de ciclo
                        if (matriculasPeriodoAnterior.Any())
                        {
                            if (estudiante.CicloActual < 10) // Máximo 10 ciclos
                            {
                                estudiante.CicloActual += 1;
                                estudiantesAvanzaron++;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al activar período", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un período académico (solo si no tiene matrículas)
        /// Solo accesible para administradores
        /// </summary>
        [HttpDelete("periodos/{id}")]
        public async Task<ActionResult> EliminarPeriodo(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos
                    .Include(p => p.Matriculas)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (periodo == null)
                    return NotFound(new { mensaje = "Período no encontrado" });

                // No permitir eliminar si tiene matrículas
                if (periodo.Matriculas.Any())
                {
                    return BadRequest(new
                    {
                        mensaje = "No se puede eliminar el período porque tiene matrículas asociadas",
                        totalMatriculas = periodo.Matriculas.Count
                    });
                }

                _context.Periodos.Remove(periodo);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Período eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar período", detalle = ex.Message });
            }
        }

        // ==========================================
        // GESTIÓN DE DOCENTES
        // ==========================================

        /// <summary>
        /// Obtiene todos los docentes con información de si tienen contraseña asignada
        /// Solo accesible para administradores
        /// </summary>
        [HttpGet("docentes")]
        public async Task<ActionResult> GetTodosDocentes()
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var docentes = await _context.Docentes
                    .Include(d => d.Cursos)
                    .OrderBy(d => d.FechaCreacion) // Ordenar por fecha de creación ascendente
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

                return Ok(docentes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener docentes", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo docente con contraseña
        /// Solo accesible para administradores
        /// </summary>
        [HttpPost("docentes")]
        public async Task<ActionResult> CrearDocente([FromBody] CrearDocenteConPasswordDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Validar contraseña mínima
                if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                {
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
                }

                // Verificar si el correo ya existe
                if (!string.IsNullOrEmpty(dto.Correo))
                {
                    var existeCorreo = await _context.Docentes
                        .AnyAsync(d => d.Correo == dto.Correo);

                    if (existeCorreo)
                    {
                        return BadRequest(new { mensaje = "Ya existe un docente con este correo electrónico" });
                    }
                }

                // Crear docente con contraseña cifrada
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

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al crear docente", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Asigna o actualiza la contraseña de un docente existente
        /// Solo accesible para administradores
        /// </summary>
        [HttpPut("docentes/{id}/password")]
        public async Task<ActionResult> AsignarPasswordDocente(int id, [FromBody] AsignarPasswordDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                // Validar contraseña mínima
                if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                {
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
                }

                var docente = await _context.Docentes.FindAsync(id);
                if (docente == null)
                {
                    return NotFound(new { mensaje = "Docente no encontrado" });
                }

                // Actualizar contraseña con hash
                docente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCrypt.Net.BCrypt.GenerateSalt(11));
                await _context.SaveChangesAsync();

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al asignar contraseña", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza información básica de un docente (sin contraseña)
        /// Solo accesible para administradores
        /// </summary>
        [HttpPut("docentes/{id}")]
        public async Task<ActionResult> ActualizarDocente(int id, [FromBody] ActualizarDocenteDto dto)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var docente = await _context.Docentes.FindAsync(id);
                if (docente == null)
                {
                    return NotFound(new { mensaje = "Docente no encontrado" });
                }

                // Verificar si el correo ya existe (excluyendo el docente actual)
                if (!string.IsNullOrEmpty(dto.Correo))
                {
                    var existeCorreo = await _context.Docentes
                        .AnyAsync(d => d.Correo == dto.Correo && d.Id != id);

                    if (existeCorreo)
                    {
                        return BadRequest(new { mensaje = "Ya existe otro docente con este correo electrónico" });
                    }
                }

                // Actualizar datos
                docente.Apellidos = dto.Apellidos;
                docente.Nombres = dto.Nombres;
                docente.Profesion = dto.Profesion;
                docente.FechaNacimiento = dto.FechaNacimiento;
                docente.Correo = dto.Correo;

                await _context.SaveChangesAsync();

                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar docente", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un docente del sistema
        /// Solo accesible para administradores
        /// </summary>
        [HttpDelete("docentes/{id}")]
        public async Task<ActionResult> EliminarDocente(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var docente = await _context.Docentes
                    .Include(d => d.Cursos)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (docente == null)
                {
                    return NotFound(new { mensaje = "Docente no encontrado" });
                }

                // Contar cursos asignados para información
                int cursosDesasignados = docente.Cursos?.Count ?? 0;

                // Si tiene cursos, desasignarlos (poner idDocente en NULL)
                if (docente.Cursos != null && docente.Cursos.Any())
                {
                    foreach (var curso in docente.Cursos)
                    {
                        curso.IdDocente = null;
                    }
                }

                // Eliminar docente
                _context.Docentes.Remove(docente);
                await _context.SaveChangesAsync();

                var mensaje = cursosDesasignados > 0
                    ? $"Docente eliminado exitosamente. Se desasignaron {cursosDesasignados} curso(s)."
                    : "Docente eliminado exitosamente";

                return Ok(new
                {
                    mensaje = mensaje,
                    cursosDesasignados = cursosDesasignados,
                    docente = new
                    {
                        id = docente.Id,
                        nombreCompleto = docente.Apellidos + ", " + docente.Nombres
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar docente", detalle = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/admin/periodos/{id}/cerrar - Cierra un período académico
        /// Utiliza el SP sp_CerrarPeriodo para calcular promedios y créditos automáticamente
        /// </summary>
        [HttpPost("periodos/{id}/cerrar")]
        public async Task<ActionResult> CerrarPeriodo(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos.FindAsync(id);
                if (periodo == null)
                {
                    return NotFound(new { mensaje = "Período no encontrado" });
                }

                if (!periodo.Activo)
                {
                    return BadRequest(new { mensaje = "El período ya está cerrado" });
                }

                // Contar estudiantes antes de cerrar para el resumen
                var estudiantesConMatriculas = await _context.Matriculas
                    .Where(m => m.IdPeriodo == id && m.Estado == "Matriculado")
                    .Select(m => m.IdEstudiante)
                    .Distinct()
                    .CountAsync();

                int totalEstudiantes = 0;
                int totalMatriculas = 0;
                int cursosAprobados = 0;
                int cursosDesaprobados = 0;

                // Ejecutar el procedimiento almacenado sp_CerrarPeriodo
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
                            // Leer el resumen del SP si existe
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

                // Calcular promovidos y retenidos basándose en las matrículas
                var estudiantesPromovidos = 0;
                var estudiantesRetenidos = 0;

                // Contar estudiantes que aprobaron todos o la mayoría de sus cursos
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
                    // Si aprobó todos los cursos o la mayoría, es promovido
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

                return Ok(new
                {
                    mensaje = "Período cerrado exitosamente",
                    estudiantesPromovidos = estudiantesPromovidos,
                    estudiantesRetenidos = estudiantesRetenidos,
                    totalEstudiantesProcesados = totalProcesados,
                    estadisticas = new
                    {
                        totalEstudiantes = totalProcesados,
                        totalMatriculas = totalMatriculas,
                        cursosAprobados = cursosAprobados,
                        cursosDesaprobados = cursosDesaprobados,
                        fechaCierre = DateTime.Now.ToString("o")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cerrar período", detalle = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/admin/periodos/{id}/abrir - Abre un nuevo período académico
        /// Utiliza el SP sp_AbrirPeriodo para avanzar ciclos automáticamente
        /// </summary>
        [HttpPost("periodos/{id}/abrir")]
        public async Task<ActionResult> AbrirPeriodo(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos.FindAsync(id);
                if (periodo == null)
                {
                    return NotFound(new { mensaje = "Período no encontrado" });
                }

                if (periodo.Activo)
                {
                    return BadRequest(new { mensaje = "El período ya está activo" });
                }

                // Ejecutar el procedimiento almacenado sp_AbrirPeriodo
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
                            // Leer el resumen de ciclos del SP
                            var resumenCiclos = new List<object>();
                            
                            if (reader.Read())
                            {
                                do
                                {
                                    var ciclo = reader.GetInt32(0);
                                    var cantidad = reader.GetInt32(1);
                                    
                                    resumenCiclos.Add(new
                                    {
                                        ciclo = ciclo,
                                        cantidadEstudiantes = cantidad
                                    });
                                } while (reader.Read());
                            }

                            return Ok(new
                            {
                                mensaje = "Período abierto exitosamente. Los estudiantes con notas del periodo anterior avanzaron de ciclo.",
                                periodoActivo = new
                                {
                                    id = periodo.Id,
                                    nombre = periodo.Nombre,
                                    anio = periodo.Anio,
                                    ciclo = periodo.Ciclo
                                },
                                resumenCiclos = resumenCiclos,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al abrir período", detalle = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/admin/periodos/{id}/validar-cierre - Valida si un período puede cerrarse
        /// Retorna lista de estudiantes sin notas completas
        /// </summary>
        [HttpGet("periodos/{id}/validar-cierre")]
        public async Task<ActionResult> ValidarCierrePeriodo(int id)
        {
            try
            {
                if (!EsAdministrador())
                    return Forbid();

                var periodo = await _context.Periodos.FindAsync(id);
                if (periodo == null)
                {
                    return NotFound(new { mensaje = "Período no encontrado" });
                }

                if (!periodo.Activo)
                {
                    return BadRequest(new { mensaje = "El período ya está cerrado" });
                }

                // Obtener todas las matrículas del período
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
                    
                    // Verificar cada tipo de evaluación
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
                            cursosPendientes = cursosPendientes
                        });
                    }
                    else
                    {
                        conNotasCompletas++;

                        // Calcular promedio estimado
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

                return Ok(new
                {
                    puedeSerCerrado = puedeSerCerrado,
                    advertencias = advertencias,
                    totalMatriculas = matriculas.Count,
                    matriculasCompletas = conNotasCompletas,
                    matriculasIncompletas = estudiantesSinNotasCompletas.Count,
                    estudiantesSinNotasCompletas = estudiantesSinNotasCompletas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al validar cierre de período", detalle = ex.Message });
            }
        }
    }
}

