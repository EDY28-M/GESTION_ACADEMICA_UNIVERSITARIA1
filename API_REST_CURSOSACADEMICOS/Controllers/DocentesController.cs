using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.DTOs;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocentesController : ControllerBase
    {
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<DocentesController> _logger;

        public DocentesController(GestionAcademicaContext context, ILogger<DocentesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verifica si el usuario autenticado es un docente
        /// </summary>
        private bool EsDocente()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "Docente";
        }

        /// <summary>
        /// Obtiene el ID del docente autenticado
        /// </summary>
        private int? ObtenerIdDocente()
        {
            var docenteIdClaim = User.FindFirst("DocenteId")?.Value;
            if (int.TryParse(docenteIdClaim, out int docenteId))
            {
                return docenteId;
            }
            return null;
        }

        // GET: api/Docentes
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<IEnumerable<DocenteDto>>> GetDocentes()
        {
            var docentes = await _context.Docentes
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

            return Ok(docentes);
        }

        // GET: api/Docentes/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<DocenteDto>> GetDocente(int id)
        {
            var docente = await _context.Docentes
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

            if (docente == null)
            {
                return NotFound($"Docente con ID {id} no encontrado");
            }

            return Ok(docente);
        }

        // POST: api/Docentes
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<DocenteDto>> PostDocente(DocenteCreateDto docenteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar si el correo ya existe
            if (!string.IsNullOrEmpty(docenteDto.Correo))
            {
                var existeCorreo = await _context.Docentes
                    .AnyAsync(d => d.Correo == docenteDto.Correo);

                if (existeCorreo)
                {
                    return BadRequest("Ya existe un docente con este correo electrónico");
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

            return CreatedAtAction(nameof(GetDocente), new { id = docente.Id }, docenteResponse);
        }

        // PUT: api/Docentes/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> PutDocente(int id, DocenteUpdateDto docenteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var docente = await _context.Docentes.FindAsync(id);
            if (docente == null)
            {
                return NotFound($"Docente con ID {id} no encontrado");
            }

            // Verificar si el correo ya existe (excluyendo el docente actual)
            if (!string.IsNullOrEmpty(docenteDto.Correo))
            {
                var existeCorreo = await _context.Docentes
                    .AnyAsync(d => d.Correo == docenteDto.Correo && d.Id != id);

                if (existeCorreo)
                {
                    return BadRequest("Ya existe otro docente con este correo electrónico");
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
                if (!DocenteExists(id))
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

        // DELETE: api/Docentes/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteDocente(int id)
        {
            var docente = await _context.Docentes.FindAsync(id);
            if (docente == null)
            {
                return NotFound($"Docente con ID {id} no encontrado");
            }

            // Verificar si tiene cursos asignados
            var tieneCursos = await _context.Cursos.AnyAsync(c => c.IdDocente == id);
            if (tieneCursos)
            {
                return BadRequest("No se puede eliminar el docente porque tiene cursos asignados");
            }

            _context.Docentes.Remove(docente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DocenteExists(int id)
        {
            return _context.Docentes.Any(e => e.Id == id);
        }

        #region Endpoints para Docente Autenticado

        /// <summary>
        /// GET /api/docentes/mis-cursos - Obtiene los cursos del docente autenticado
        /// OPTIMIZADO: Consultas eficientes sin N+1
        /// </summary>
        [HttpGet("mis-cursos")]
        [Authorize]
        public async Task<IActionResult> GetMisCursos()
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Obtener período activo
                var periodoActivo = await _context.Periodos
                    .AsNoTracking()
                    .Where(p => p.Activo == true)
                    .OrderByDescending(p => p.FechaInicio)
                    .FirstOrDefaultAsync();

                if (periodoActivo == null)
                {
                    return Ok(new List<CursoDocenteDto>());
                }

                // Obtener cursos del docente
                var cursos = await _context.Cursos
                    .AsNoTracking()
                    .Where(c => c.IdDocente == docenteId)
                    .ToListAsync();

                if (!cursos.Any())
                {
                    return Ok(new List<CursoDocenteDto>());
                }

                var cursosIds = cursos.Select(c => c.Id).ToList();

                // OPTIMIZACIÓN: Traer todas las matrículas de los cursos del docente en UNA consulta
                var matriculasConNotas = await _context.Matriculas
                    .AsNoTracking()
                    .Where(m => cursosIds.Contains(m.IdCurso) && 
                               m.IdPeriodo == periodoActivo.Id && 
                               m.Estado == "Matriculado")
                    .Include(m => m.Notas)
                    .ToListAsync();

                // OPTIMIZACIÓN: Traer todas las asistencias en UNA consulta
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

                // Construir DTOs en memoria (muy rápido)
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

                return Ok(cursosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cursos del docente");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{id}/estudiantes - Obtiene los estudiantes de un curso
        /// OPTIMIZADO: Sin consultas N+1
        /// </summary>
        [HttpGet("cursos/{idCurso}/estudiantes")]
        [Authorize]
        public async Task<IActionResult> GetEstudiantesCurso(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Obtener período activo
                var periodoActivo = await _context.Periodos
                    .AsNoTracking()
                    .Where(p => p.Activo == true)
                    .OrderByDescending(p => p.FechaInicio)
                    .FirstOrDefaultAsync();

                if (periodoActivo == null)
                {
                    return Ok(new { 
                        estudiantes = new List<EstudianteCursoDto>(),
                        mensaje = "No hay período activo. Configure un período activo en el sistema.",
                        hayPeriodoActivo = false
                    });
                }

                // OPTIMIZACIÓN: Una sola consulta con todas las matrículas, estudiantes y notas
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
                    var totalMatriculasOtrosPeriodos = await _context.Matriculas
                        .CountAsync(m => m.IdCurso == idCurso);

                    return Ok(new { 
                        estudiantes = new List<EstudianteCursoDto>(),
                        mensaje = totalMatriculasOtrosPeriodos > 0 
                            ? $"No hay estudiantes matriculados en este curso para el período activo '{periodoActivo.Nombre}'."
                            : "No hay estudiantes matriculados en este curso.",
                        hayPeriodoActivo = true,
                        periodoActivo = periodoActivo.Nombre,
                        totalMatriculasOtrosPeriodos = totalMatriculasOtrosPeriodos
                    });
                }

                // OPTIMIZACIÓN: Traer asistencias en una sola consulta
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

                // Mapear a DTOs en memoria (muy rápido, sin queries adicionales)
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
                            notasDict[nota.TipoEvaluacion] = nota.NotaValor;
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

                return Ok(estudiantes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estudiantes del curso");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/cursos/{idCurso}/notas - Registrar o actualizar notas de un estudiante
        /// </summary>
        [HttpPost("cursos/{idCurso}/notas")]
        [Authorize]
        public async Task<IActionResult> RegistrarNotas(int idCurso, [FromBody] System.Text.Json.JsonElement notasJson)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Extraer idMatricula del JSON
                if (!notasJson.TryGetProperty("idMatricula", out var idMatriculaElement))
                {
                    return BadRequest(new { message = "idMatricula es requerido" });
                }
                int idMatricula = idMatriculaElement.GetInt32();

                // Verificar que la matrícula existe y pertenece al curso
                var matricula = await _context.Matriculas
                    .Include(m => m.Notas)
                    .FirstOrDefaultAsync(m => m.Id == idMatricula && m.IdCurso == idCurso);

                if (matricula == null)
                {
                    return NotFound(new { message = "Matrícula no encontrada" });
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
                    // Registrar notas con la configuración por defecto (hardcodeada)
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
                    // Usar los tipos de evaluación configurados dinámicamente
                    foreach (var tipoEval in tiposEvaluacion)
                    {
                        // Usar el nombre EXACTO del tipo de evaluación como clave JSON
                        var nombreTipo = tipoEval.Nombre;
                        
                        // Log para debug
                        _logger.LogInformation($"Procesando tipo evaluación: {nombreTipo}, Peso: {tipoEval.Peso}");
                        
                        // Intentar obtener el valor del JSON usando el nombre EXACTO
                        if (notasJson.TryGetProperty(nombreTipo, out var valorElement))
                        {
                            decimal? valor = null;
                            if (valorElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                            {
                                valor = valorElement.GetDecimal();
                                _logger.LogInformation($"✅ Valor encontrado para '{nombreTipo}': {valor}");
                            }
                            
                            await RegistrarOActualizarNota(
                                matricula.Id, 
                                nombreTipo,  // Usar el nombre EXACTO configurado
                                valor, 
                                tipoEval.Peso,  // Usar el peso decimal directamente
                                observaciones
                            );
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ No se encontró el campo '{nombreTipo}' en el JSON");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Calcular promedio final
                var promedioFinal = CalcularPromedioFinalConRedondeo(matricula.Id);

                return Ok(new { 
                    message = "Notas registradas correctamente",
                    promedioFinal = promedioFinal,
                    aprobado = promedioFinal >= 11
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar notas");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/tipos-evaluacion - Obtener tipos de evaluación configurados para un curso
        /// </summary>
        [HttpGet("cursos/{idCurso}/tipos-evaluacion")]
        [Authorize]
        public async Task<IActionResult> ObtenerTiposEvaluacion(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Obtener tipos de evaluación configurados
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

                // Si no hay tipos configurados, devolver la configuración por defecto
                if (tiposEvaluacion.Count == 0)
                {
                    tiposEvaluacion = new List<TipoEvaluacionDto>
                    {
                        new TipoEvaluacionDto { Id = 0, Nombre = "Parcial 1", Peso = 10, Orden = 1, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Parcial 2", Peso = 10, Orden = 2, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Prácticas", Peso = 20, Orden = 3, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Medio Curso", Peso = 20, Orden = 4, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Examen Final", Peso = 20, Orden = 5, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Actitud", Peso = 5, Orden = 6, Activo = true },
                        new TipoEvaluacionDto { Id = 0, Nombre = "Trabajos", Peso = 15, Orden = 7, Activo = true }
                    };
                }

                return Ok(tiposEvaluacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de evaluación");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/cursos/{idCurso}/tipos-evaluacion - Configurar tipos de evaluación para un curso
        /// </summary>
        [HttpPost("cursos/{idCurso}/tipos-evaluacion")]
        [Authorize]
        public async Task<IActionResult> ConfigurarTiposEvaluacion(int idCurso, [FromBody] ConfigurarTiposEvaluacionDto configDto)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Validar que los pesos sumen 100
                var pesoTotal = configDto.TiposEvaluacion.Where(t => t.Activo).Sum(t => t.Peso);
                if (Math.Abs(pesoTotal - 100) > 0.01m)
                {
                    return BadRequest(new { message = $"Los pesos deben sumar 100%. Suma actual: {pesoTotal}%" });
                }

                // Obtener tipos existentes
                var tiposExistentes = await _context.TiposEvaluacion
                    .Where(t => t.IdCurso == idCurso)
                    .ToListAsync();

                // Si no hay tipos configurados aún, esta es la primera configuración
                // Migrar notas existentes con nombres antiguos
                bool esPrimeraConfiguracion = tiposExistentes.Count == 0;
                
                if (esPrimeraConfiguracion)
                {
                    _logger.LogInformation($"Primera configuración de evaluaciones para curso {idCurso}. Migrando notas existentes...");
                    
                    // Obtener todas las matrículas del curso
                    var matriculasCurso = await _context.Matriculas
                        .Where(m => m.IdCurso == idCurso)
                        .Select(m => m.Id)
                        .ToListAsync();
                    
                    // Mapeo de nombres antiguos a nuevos según el orden
                    var mapeoMigracion = new Dictionary<int, List<string>>
                    {
                        { 0, new List<string> { "Parcial 1", "parcial 1", "EP1", "Examen Parcial 1" } },  // Orden 1
                        { 1, new List<string> { "Parcial 2", "parcial 2", "EP2", "Examen Parcial 2" } },  // Orden 2
                        { 2, new List<string> { "Prácticas", "Práctica", "practicas", "practica", "PR" } }, // Orden 3
                        { 3, new List<string> { "Medio Curso", "medio curso", "MedioCurso", "MC" } },     // Orden 4
                        { 4, new List<string> { "Examen Final", "examen final", "ExamenFinal", "EF" } },  // Orden 5
                        { 5, new List<string> { "Actitud", "actitud", "EA" } },                           // Orden 6
                        { 6, new List<string> { "Trabajos", "trabajos", "Trabajo encargado", "trabajo encargado", "TE", "T" } } // Orden 7
                    };
                    
                    for (int i = 0; i < configDto.TiposEvaluacion.Count && i < mapeoMigracion.Count; i++)
                    {
                        var tipoDto = configDto.TiposEvaluacion[i];
                        var nombresAntiguos = mapeoMigracion[i];
                        
                        // Actualizar todas las notas que coincidan con alguno de los nombres antiguos
                        var notasAMigrar = await _context.Notas
                            .Where(n => matriculasCurso.Contains(n.IdMatricula) && 
                                   nombresAntiguos.Contains(n.TipoEvaluacion))
                            .ToListAsync();
                        
                        foreach (var nota in notasAMigrar)
                        {
                            _logger.LogInformation($"Migrando nota: '{nota.TipoEvaluacion}' -> '{tipoDto.Nombre}'");
                            nota.TipoEvaluacion = tipoDto.Nombre;
                            nota.Peso = tipoDto.Peso;
                        }
                        
                        _logger.LogInformation($"Se migraron {notasAMigrar.Count} notas a '{tipoDto.Nombre}'");
                    }
                }

                // Procesar cada tipo
                foreach (var tipoDto in configDto.TiposEvaluacion)
                {
                    if (tipoDto.Id == 0)
                    {
                        // Crear nuevo tipo
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
                        // Actualizar tipo existente
                        var tipoExistente = tiposExistentes.FirstOrDefault(t => t.Id == tipoDto.Id);
                        if (tipoExistente != null)
                        {
                            var nombreAnterior = tipoExistente.Nombre;
                            var nombreNuevo = tipoDto.Nombre;
                            var pesoAnterior = tipoExistente.Peso;
                            var pesoNuevo = tipoDto.Peso;
                            
                            // Obtener todas las matrículas del curso
                            var matriculasCurso = await _context.Matriculas
                                .Where(m => m.IdCurso == idCurso)
                                .Select(m => m.Id)
                                .ToListAsync();
                            
                            // Si el nombre cambió, actualizar todas las notas de este curso que tengan el nombre anterior
                            if (nombreAnterior != nombreNuevo)
                            {
                                _logger.LogInformation($"🔄 Migrando notas de '{nombreAnterior}' a '{nombreNuevo}' en curso {idCurso}");
                                
                                // Actualizar las notas en la base de datos
                                var notasAActualizar = await _context.Notas
                                    .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == nombreAnterior)
                                    .ToListAsync();
                                
                                foreach (var nota in notasAActualizar)
                                {
                                    _logger.LogInformation($"  ✓ Nota ID {nota.Id}: '{nota.TipoEvaluacion}' -> '{nombreNuevo}', Peso: {nota.Peso}% -> {pesoNuevo}%");
                                    nota.TipoEvaluacion = nombreNuevo;
                                    nota.Peso = pesoNuevo;
                                }
                                
                                _logger.LogInformation($"✅ Se actualizaron {notasAActualizar.Count} notas de '{nombreAnterior}' a '{nombreNuevo}'");
                            }
                            // Si solo el peso cambió, actualizar todas las notas con ese nombre
                            else if (Math.Abs(pesoAnterior - pesoNuevo) > 0.01m)
                            {
                                _logger.LogInformation($"🔄 Actualizando pesos de '{nombreAnterior}' de {pesoAnterior}% a {pesoNuevo}% en curso {idCurso}");
                                
                                var notasAActualizar = await _context.Notas
                                    .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == nombreAnterior)
                                    .ToListAsync();
                                
                                foreach (var nota in notasAActualizar)
                                {
                                    _logger.LogInformation($"  ✓ Nota ID {nota.Id}: Peso {nota.Peso}% -> {pesoNuevo}%");
                                    nota.Peso = pesoNuevo;
                                }
                                
                                _logger.LogInformation($"✅ Se actualizaron {notasAActualizar.Count} pesos para '{nombreAnterior}'");
                            }
                            
                            // Actualizar el tipo de evaluación
                            tipoExistente.Nombre = nombreNuevo;
                            tipoExistente.Peso = pesoNuevo;
                            tipoExistente.Orden = tipoDto.Orden;
                            tipoExistente.Activo = tipoDto.Activo;
                        }
                    }
                }

                // Eliminar tipos que ya no están en la configuración
                var idsConfiguracion = configDto.TiposEvaluacion.Where(t => t.Id > 0).Select(t => t.Id).ToList();
                var tiposAEliminar = tiposExistentes.Where(t => !idsConfiguracion.Contains(t.Id)).ToList();
                
                // Antes de eliminar los tipos, eliminar las notas asociadas
                if (tiposAEliminar.Any())
                {
                    _logger.LogInformation($"🗑️ Eliminando {tiposAEliminar.Count} tipos de evaluación y sus notas asociadas");
                    
                    var matriculasCurso = await _context.Matriculas
                        .Where(m => m.IdCurso == idCurso)
                        .Select(m => m.Id)
                        .ToListAsync();
                    
                    foreach (var tipoEliminar in tiposAEliminar)
                    {
                        _logger.LogInformation($"  🗑️ Eliminando tipo: '{tipoEliminar.Nombre}'");
                        
                        // Eliminar todas las notas con este tipo de evaluación
                        var notasAEliminar = await _context.Notas
                            .Where(n => matriculasCurso.Contains(n.IdMatricula) && n.TipoEvaluacion == tipoEliminar.Nombre)
                            .ToListAsync();
                        
                        _context.Notas.RemoveRange(notasAEliminar);
                        _logger.LogInformation($"    ✓ Eliminadas {notasAEliminar.Count} notas de tipo '{tipoEliminar.Nombre}'");
                    }
                }
                
                _context.TiposEvaluacion.RemoveRange(tiposAEliminar);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Tipos de evaluación configurados correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar tipos de evaluación");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/docentes/asistencia - Registrar asistencia masiva para una fecha
        /// </summary>
        [HttpPost("asistencia")]
        [Authorize]
        public async Task<IActionResult> RegistrarAsistencia([FromBody] RegistrarAsistenciasMasivasDto asistenciaDto)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(asistenciaDto.IdCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Registrar cada asistencia
                foreach (var asistencia in asistenciaDto.Asistencias)
                {
                    // Verificar si ya existe registro para esa fecha y tipo de clase
                    var asistenciaExistente = await _context.Asistencias
                        .FirstOrDefaultAsync(a => a.IdEstudiante == asistencia.IdEstudiante &&
                                                 a.IdCurso == asistenciaDto.IdCurso &&
                                                 a.Fecha.Date == asistenciaDto.Fecha.Date &&
                                                 a.TipoClase == asistenciaDto.TipoClase);

                    if (asistenciaExistente != null)
                    {
                        // Actualizar
                        asistenciaExistente.Presente = asistencia.Presente;
                        asistenciaExistente.Observaciones = asistencia.Observaciones;
                        asistenciaExistente.FechaRegistro = DateTime.Now;
                    }
                    else
                    {
                        // Crear nuevo
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

                return Ok(new { message = "Asistencias registradas correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar asistencias");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/asistencia - Obtener resumen de asistencia del curso
        /// </summary>
        [HttpGet("cursos/{idCurso}/asistencia")]
        [Authorize]
        public async Task<IActionResult> GetAsistenciaCurso(int idCurso, [FromQuery] DateTime? fecha = null, [FromQuery] string? tipoClase = null)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                var query = _context.Asistencias
                    .Where(a => a.IdCurso == idCurso);

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

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener asistencias del curso");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/docentes/cursos/{idCurso}/asistencia/resumen - Obtener resumen de asistencia por estudiante
        /// </summary>
        [HttpGet("cursos/{idCurso}/asistencia/resumen")]
        [Authorize]
        public async Task<IActionResult> GetResumenAsistencia(int idCurso)
        {
            try
            {
                if (!EsDocente())
                {
                    return Forbid();
                }

                var docenteId = ObtenerIdDocente();
                if (docenteId == null)
                {
                    return Unauthorized(new { message = "No se pudo identificar al docente" });
                }

                // Verificar que el curso pertenece al docente
                var curso = await _context.Cursos.FindAsync(idCurso);
                if (curso == null)
                {
                    return NotFound(new { message = "Curso no encontrado" });
                }

                if (curso.IdDocente != docenteId)
                {
                    return Forbid();
                }

                // Obtener período activo
                var periodoActivo = await _context.Periodos
                    .Where(p => p.Activo == true)
                    .OrderByDescending(p => p.FechaInicio)
                    .FirstOrDefaultAsync();

                if (periodoActivo == null)
                {
                    return Ok(new List<ResumenAsistenciaDto>());
                }

                // Obtener estudiantes matriculados
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
                        TotalClases = totalClases,
                        AsistenciasPresentes = presentes,
                        Faltas = faltas,
                        PorcentajeAsistencia = porcentaje,
                        DetalleAsistencias = asistencias
                    });
                }

                return Ok(resumen.OrderBy(r => r.NombreEstudiante).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de asistencias");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        #endregion

        #region Métodos auxiliares para Docente

        /// <summary>
        /// Registra o actualiza una nota desde JSON dinámico
        /// </summary>
        private async Task RegistrarOActualizarNotaDesdeJson(
            int idMatricula,
            string tipoEvaluacion,
            string campoJson,
            System.Text.Json.JsonElement notasJson,
            int peso,
            string? observaciones)
        {
            var valor = ObtenerValorNotaDesdeJson(notasJson, campoJson, tipoEvaluacion);

            await RegistrarOActualizarNota(idMatricula, tipoEvaluacion, valor, peso, observaciones);
        }

        private decimal? ObtenerValorNotaDesdeJson(
            System.Text.Json.JsonElement notasJson,
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
                if (propiedad.Value.ValueKind != System.Text.Json.JsonValueKind.Number)
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

        /// <summary>
        /// Registra o actualiza una nota específica
        /// </summary>
        private async Task RegistrarOActualizarNota(int idMatricula, string tipoEvaluacion, decimal? notaValor, decimal peso, string? observaciones)
        {
            if (!notaValor.HasValue) return;

            _logger.LogInformation($"Guardando nota: Matrícula={idMatricula}, Tipo='{tipoEvaluacion}', Valor={notaValor}, Peso={peso}");

            var notaExistente = await _context.Notas
                .FirstOrDefaultAsync(n => n.IdMatricula == idMatricula && n.TipoEvaluacion == tipoEvaluacion);

            if (notaExistente != null)
            {
                _logger.LogInformation($"Actualizando nota existente ID={notaExistente.Id}");
                notaExistente.NotaValor = notaValor.Value;
                notaExistente.Peso = peso;
                notaExistente.Fecha = DateTime.Now;
                notaExistente.Observaciones = observaciones;
            }
            else
            {
                _logger.LogInformation($"Creando nueva nota para tipo '{tipoEvaluacion}'");
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

        /// <summary>
        /// Calcula el promedio de una matrícula
        /// </summary>
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

            _logger.LogInformation($"=== Calculando promedio para matrícula {idMatricula} ===");
            foreach (var nota in notas)
            {
                var contribucion = nota.NotaValor * (nota.Peso / 100m);
                promedioPonderado += contribucion;
                pesoTotal += nota.Peso;
                
                _logger.LogInformation($"  {nota.TipoEvaluacion}: Nota={nota.NotaValor}, Peso={nota.Peso}%, Contribución={contribucion:F2}");
            }

            _logger.LogInformation($"  TOTAL: PesoAcumulado={pesoTotal}%, PromedioFinal={promedioPonderado:F2}");

            return promedioPonderado;
        }

        /// <summary>
        /// Calcula el promedio final con redondeo a número entero
        /// </summary>
        private decimal? CalcularPromedioFinalConRedondeo(int idMatricula)
        {
            var promedio = CalcularPromedioMatricula(idMatricula);
            if (promedio == 0) return null;

            return Math.Round(promedio, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calcula el porcentaje de asistencia de un estudiante en un curso
        /// </summary>
        private decimal CalcularPorcentajeAsistencia(int idEstudiante, int idCurso)
        {
            var periodoActivo = _context.Periodos
                .Where(p => p.Activo == true)
                .OrderByDescending(p => p.FechaInicio)
                .FirstOrDefault();

            if (periodoActivo == null)
            {
                return 0;
            }

            var matriculaActiva = _context.Matriculas
                .FirstOrDefault(m => m.IdEstudiante == idEstudiante && 
                                    m.IdCurso == idCurso && 
                                    m.IdPeriodo == periodoActivo.Id &&
                                    m.Estado == "Matriculado");

            if (matriculaActiva == null)
            {
                return 0;
            }

            var totalClases = _context.Asistencias
                .Count(a => a.IdEstudiante == idEstudiante && a.IdCurso == idCurso);

            if (totalClases == 0) return 0;

            var presentes = _context.Asistencias
                .Count(a => a.IdEstudiante == idEstudiante && a.IdCurso == idCurso && a.Presente);

            return (decimal)presentes / totalClases * 100;
        }

        /// <summary>
        /// Obtiene las notas de un estudiante
        /// </summary>
        private object? ObtenerNotasEstudiante(int idMatricula)
        {
            var matricula = _context.Matriculas.Find(idMatricula);
            if (matricula == null || matricula.Estado != "Matriculado")
            {
                return null;
            }

            var notas = _context.Notas.Where(n => n.IdMatricula == idMatricula).ToList();
            if (notas.Count == 0) return null;

            var notasDetalle = new Dictionary<string, object>();
            
            foreach (var nota in notas)
            {
                notasDetalle[nota.TipoEvaluacion] = nota.NotaValor;
            }

            notasDetalle["promedioCalculado"] = CalcularPromedioMatricula(idMatricula);
            notasDetalle["promedioFinal"] = CalcularPromedioFinalConRedondeo(idMatricula) ?? 0;

            return notasDetalle;
        }

        /// <summary>
        /// Convierte un string a camelCase
        /// </summary>
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
}
