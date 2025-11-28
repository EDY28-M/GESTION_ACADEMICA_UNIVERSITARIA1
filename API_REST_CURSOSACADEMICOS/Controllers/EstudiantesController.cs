using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly IEstudianteService _estudianteService;
        private readonly GestionAcademicaContext _context;

        public EstudiantesController(IEstudianteService estudianteService, GestionAcademicaContext context)
        {
            _estudianteService = estudianteService;
            _context = context;
        }

        private int GetUsuarioId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Usuario no autenticado");

            return int.Parse(userIdClaim);
        }

        /// <summary>
        /// Obtiene todos los estudiantes (Solo Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<List<EstudianteDto>>> GetAll()
        {
            try
            {
                var estudiantes = await _context.Estudiantes
                    .Select(e => new EstudianteDto
                    {
                        Id = e.Id,
                        Codigo = e.Codigo,
                        Nombres = e.Nombres,
                        Apellidos = e.Apellidos,
                        Dni = e.Dni,
                        Correo = e.Correo,
                        Telefono = e.Telefono,
                        Direccion = e.Direccion,
                        CicloActual = e.CicloActual,
                        CreditosAcumulados = e.CreditosAcumulados,
                        PromedioAcumulado = e.PromedioAcumulado,
                        PromedioSemestral = e.PromedioSemestral,
                        Estado = e.Estado,
                        Carrera = e.Carrera
                    })
                    .OrderBy(e => e.Apellidos)
                    .ThenBy(e => e.Nombres)
                    .ToListAsync();

                return Ok(estudiantes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener estudiantes", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un estudiante por ID (Solo Admin)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<EstudianteDto>> GetById(int id)
        {
            try
            {
                var estudiante = await _context.Estudiantes
                    .Where(e => e.Id == id)
                    .Select(e => new EstudianteDto
                    {
                        Id = e.Id,
                        Codigo = e.Codigo,
                        Nombres = e.Nombres,
                        Apellidos = e.Apellidos,
                        Dni = e.Dni,
                        Correo = e.Correo,
                        Telefono = e.Telefono,
                        Direccion = e.Direccion,
                        CicloActual = e.CicloActual,
                        CreditosAcumulados = e.CreditosAcumulados,
                        PromedioAcumulado = e.PromedioAcumulado,
                        PromedioSemestral = e.PromedioSemestral,
                        Estado = e.Estado,
                        Carrera = e.Carrera
                    })
                    .FirstOrDefaultAsync();

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                return Ok(estudiante);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener estudiante", detalle = ex.Message });
            }
        }

        [HttpGet("perfil")]
        public async Task<ActionResult<EstudianteDto>> GetPerfil()
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                return Ok(estudiante);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("cursos-disponibles")]
        public async Task<ActionResult<List<CursoDisponibleDto>>> GetCursosDisponibles([FromQuery] int? idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                // Si no se especifica período, usar el activo
                if (!idPeriodo.HasValue)
                {
                    var periodoActivo = await _estudianteService.GetPeriodoActivoAsync();
                    if (periodoActivo == null)
                        return BadRequest(new { mensaje = "No hay período activo" });

                    idPeriodo = periodoActivo.Id;
                }

                // Usar el nuevo método que utiliza la función SQL fn_CursosDisponibles
                // Esto automáticamente filtra por ciclo actual y excluye cursos aprobados
                var cursos = await _estudianteService.GetCursosDisponiblesPorEstudianteAsync(estudiante.Id);

                // Verificar cuáles ya están matriculados en el periodo actual
                var cursosMatriculados = await _context.Matriculas
                    .Where(m => m.IdEstudiante == estudiante.Id && 
                               m.IdPeriodo == idPeriodo.Value && 
                               m.Estado == "Matriculado")
                    .Select(m => m.IdCurso)
                    .ToListAsync();

                // Actualizar el estado de matriculado
                foreach (var curso in cursos)
                {
                    if (cursosMatriculados.Contains(curso.Id))
                    {
                        curso.YaMatriculado = true;
                        curso.Disponible = false;
                        curso.MotivoNoDisponible = "Ya estás matriculado en este curso";
                    }
                }

                return Ok(cursos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("mis-cursos")]
        public async Task<ActionResult<List<MatriculaDto>>> GetMisCursos([FromQuery] int? idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                var matriculas = await _estudianteService.GetMisCursosAsync(estudiante.Id, idPeriodo);

                return Ok(matriculas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("matricular")]
        public async Task<ActionResult<MatriculaDto>> Matricular([FromBody] MatricularDto dto)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                var matricula = await _estudianteService.MatricularAsync(estudiante.Id, dto);

                return Ok(matricula);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpDelete("retirar/{idMatricula}")]
        public async Task<ActionResult> Retirar(int idMatricula)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                await _estudianteService.RetirarAsync(idMatricula, estudiante.Id);

                return Ok(new { mensaje = "Curso retirado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("notas")]
        public async Task<ActionResult> GetNotas([FromQuery] int? idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudianteDto = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudianteDto == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                var notas = await _estudianteService.GetNotasAsync(estudianteDto.Id, idPeriodo);

                // Calcular promedio general (excluye cursos retirados)
                var matriculas = await _estudianteService.GetMisCursosAsync(estudianteDto.Id, idPeriodo);
                var cursosActivos = matriculas
                    .Where(m => m.Estado != "Retirado" && m.PromedioFinal.HasValue)
                    .ToList();

                decimal promedioGeneral = 0;
                if (cursosActivos.Any())
                {
                    promedioGeneral = cursosActivos.Average(m => m.PromedioFinal!.Value);
                }

                var resultado = new
                {
                    notas,
                    promedioGeneral = Math.Round(promedioGeneral, 2),
                    promedioAcumulado = estudianteDto.PromedioAcumulado,
                    promedioSemestral = estudianteDto.PromedioSemestral,
                    creditosAcumulados = estudianteDto.CreditosAcumulados
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // Nuevo endpoint: devuelve las notas junto con el promedioGeneral (excluye cursos retirados)
        [HttpGet("notas/estadisticas")]
        public async Task<ActionResult> GetNotasConEstadisticas([FromQuery] int? idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudianteDto = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudianteDto == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                // Obtener notas filtradas (el servicio ya excluye cursos retirados)
                var notas = await _estudianteService.GetNotasAsync(estudianteDto.Id, idPeriodo);

                // Obtener matrículas para calcular promedio por curso
                var matriculas = await _estudianteService.GetMisCursosAsync(estudianteDto.Id, idPeriodo);

                var cursosActivos = matriculas
                    .Where(m => m.Estado != "Retirado" && m.PromedioFinal.HasValue)
                    .ToList();

                decimal promedioGeneral = 0;
                if (cursosActivos.Any())
                {
                    promedioGeneral = cursosActivos.Average(m => m.PromedioFinal!.Value);
                }

                var resultado = new
                {
                    notas,
                    promedioGeneral = Math.Round(promedioGeneral, 2),
                    promedioAcumulado = estudianteDto.PromedioAcumulado,
                    promedioSemestral = estudianteDto.PromedioSemestral,
                    creditosAcumulados = estudianteDto.CreditosAcumulados
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("periodos")]
        public async Task<ActionResult<List<PeriodoDto>>> GetPeriodos()
        {
            try
            {
                var periodos = await _estudianteService.GetPeriodosAsync();
                return Ok(periodos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("periodo-activo")]
        public async Task<ActionResult<PeriodoDto>> GetPeriodoActivo()
        {
            try
            {
                var periodo = await _estudianteService.GetPeriodoActivoAsync();

                if (periodo == null)
                    return NotFound(new { mensaje = "No hay período activo" });

                return Ok(periodo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticas([FromQuery] int? idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudianteDto = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudianteDto == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                // Obtener el estudiante completo del contexto para acceder a PromedioPonderado
                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.Id == estudianteDto.Id);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                // Obtener todas las matrículas (filtrando período si se especifica)
                var matriculas = await _estudianteService.GetMisCursosAsync(estudianteDto.Id, idPeriodo);

                // IMPORTANTE: Filtrar cursos retirados para el cálculo del promedio
                var cursosActivos = matriculas
                    .Where(m => m.Estado != "Retirado" && m.PromedioFinal.HasValue)
                    .ToList();

                decimal promedioGeneral = 0;
                if (cursosActivos.Any())
                {
                    promedioGeneral = cursosActivos.Average(m => m.PromedioFinal!.Value);
                }

                var estadisticas = new
                {
                    promedioGeneral = Math.Round(promedioGeneral, 2),
                    totalCursos = matriculas.Count,
                    cursosMatriculados = matriculas.Count(m => m.Estado == "Matriculado"),
                    cursosAprobados = matriculas.Count(m => m.Estado == "Aprobado" || 
                                                           (m.Estado == "Matriculado" && m.PromedioFinal >= 10.5m)),
                    cursosDesaprobados = matriculas.Count(m => m.Estado == "Desaprobado" || 
                                                              (m.Estado == "Matriculado" && m.PromedioFinal < 10.5m && m.PromedioFinal > 0)),
                    cursosRetirados = matriculas.Count(m => m.Estado == "Retirado"),
                    creditosAcumulados = estudiante.CreditosAcumulados,
                    promedioAcumulado = estudiante.PromedioAcumulado,
                    promedioSemestral = estudiante.PromedioSemestral
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpGet("registro-notas")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<RegistroNotasDto>> GetRegistroNotas()
        {
            try
            {
                var usuarioId = GetUsuarioId();
                
                // Obtener el estudiante
                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                var registroNotas = await _estudianteService.GetRegistroNotasAsync(estudiante.Id);

                return Ok(registroNotas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener registro de notas", detalle = ex.Message });
            }
        }

        [HttpGet("registro-notas/{idPeriodo}")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<SemestreRegistroDto>> GetRegistroNotasPorPeriodo(int idPeriodo)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                
                // Obtener el estudiante
                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                var registroNotas = await _estudianteService.GetRegistroNotasAsync(estudiante.Id);
                var semestreEspecifico = registroNotas.Semestres.FirstOrDefault(s => s.IdPeriodo == idPeriodo);

                if (semestreEspecifico == null)
                    return NotFound(new { mensaje = "No se encontraron registros para el periodo especificado" });

                return Ok(semestreEspecifico);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener registro de notas", detalle = ex.Message });
            }
        }

        [HttpGet("verificar-prerequisitos/{idCurso}")]
        public async Task<ActionResult<object>> VerificarPrerequisitos(int idCurso)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Perfil de estudiante no encontrado" });

                // Obtener los prerequisitos del curso
                var prerequisitos = await _context.CursoPrerequisitos
                    .Include(cp => cp.Prerequisito)
                    .Where(cp => cp.IdCurso == idCurso)
                    .ToListAsync();

                if (!prerequisitos.Any())
                {
                    return Ok(new 
                    { 
                        cumplePrerequisitos = true, 
                        mensaje = "Este curso no tiene prerequisitos",
                        prerequisitosFaltantes = new List<object>()
                    });
                }

                // Verificar cuáles prerequisitos ha aprobado el estudiante
                // Un curso se considera aprobado si tiene PromedioFinal >= 11
                var cursosAprobados = await _context.Matriculas
                    .Where(m => m.IdEstudiante == estudiante.Id && 
                               m.PromedioFinal.HasValue &&
                               m.PromedioFinal.Value >= 11)
                    .Select(m => m.IdCurso)
                    .ToListAsync();

                var prerequisitosFaltantes = new List<object>();
                bool cumpleTodos = true;

                foreach (var prereq in prerequisitos)
                {
                    var aprobado = cursosAprobados.Contains(prereq.IdCursoPrerequisito);
                    if (!aprobado)
                    {
                        cumpleTodos = false;
                        
                        // Verificar si lo cursó pero no aprobó
                        var matriculaCurso = await _context.Matriculas
                            .Where(m => m.IdEstudiante == estudiante.Id && 
                                   m.IdCurso == prereq.IdCursoPrerequisito)
                            .OrderByDescending(m => m.FechaMatricula)
                            .FirstOrDefaultAsync();
                        
                        string estado = "No cursado";
                        decimal? nota = null;
                        
                        if (matriculaCurso != null && matriculaCurso.PromedioFinal.HasValue)
                        {
                            nota = matriculaCurso.PromedioFinal.Value;
                            estado = nota >= 11 ? "Aprobado" : $"Reprobado (Nota: {nota})";
                        }
                        else if (matriculaCurso != null)
                        {
                            estado = "Cursando";
                        }
                        
                        prerequisitosFaltantes.Add(new
                        {
                            id = prereq.Prerequisito.Id,
                            codigo = prereq.Prerequisito.Codigo,
                            nombre = prereq.Prerequisito.NombreCurso,
                            ciclo = prereq.Prerequisito.Ciclo,
                            estado = estado,
                            nota = nota
                        });
                    }
                }

                return Ok(new
                {
                    cumplePrerequisitos = cumpleTodos,
                    mensaje = cumpleTodos 
                        ? "Cumples con todos los prerequisitos" 
                        : "Te faltan aprobar algunos prerequisitos",
                    prerequisitosFaltantes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al verificar prerequisitos", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el orden de mérito por promoción
        /// </summary>
        [HttpGet("orden-merito")]
        public async Task<ActionResult<IEnumerable<OrdenMeritoDto>>> GetOrdenMerito([FromQuery] string? promocion = null)
        {
            try
            {
                var query = promocion == null
                    ? "SELECT * FROM vw_OrdenMerito ORDER BY promocion DESC, posicion ASC"
                    : $"SELECT * FROM vw_OrdenMerito WHERE promocion = '{promocion}' ORDER BY posicion ASC";

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = query;

                var ordenMerito = new List<OrdenMeritoDto>();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ordenMerito.Add(new OrdenMeritoDto
                        {
                            Posicion = Convert.ToInt32(reader.GetInt64(reader.GetOrdinal("posicion"))),
                            Codigo = reader.GetString(reader.GetOrdinal("codigo")),
                            Nombres = reader.GetString(reader.GetOrdinal("nombres")),
                            Apellidos = reader.GetString(reader.GetOrdinal("apellidos")),
                            Promocion = reader.IsDBNull(reader.GetOrdinal("promocion")) ? "" : reader.GetString(reader.GetOrdinal("promocion")),
                            Semestre = reader.GetInt32(reader.GetOrdinal("ciclo_actual")),
                            CreditosLlevadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_llevados_semestre")),
                            CreditosAprobadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_aprobados_semestre")),
                            TotalCreditosLlevados = reader.GetInt32(reader.GetOrdinal("total_creditos_llevados")),
                            TotalCreditosAprobados = reader.GetInt32(reader.GetOrdinal("total_creditos_aprobados")),
                            PromedioPonderadoSemestral = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_semestral")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_semestral")),
                            PromedioPonderadoAcumulado = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_acumulado")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_acumulado")),
                            RangoMerito = reader.IsDBNull(reader.GetOrdinal("rango_merito")) ? "" : reader.GetString(reader.GetOrdinal("rango_merito")),
                            TotalEstudiantes = reader.GetInt32(reader.GetOrdinal("total_estudiantes")),
                            PeriodoNombre = reader.IsDBNull(reader.GetOrdinal("periodo_nombre")) ? null : reader.GetString(reader.GetOrdinal("periodo_nombre")),
                            EstadoPeriodo = reader.IsDBNull(reader.GetOrdinal("estado_periodo")) ? null : reader.GetString(reader.GetOrdinal("estado_periodo"))
                        });
                    }
                }

                await connection.CloseAsync();

                return Ok(ordenMerito);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al obtener orden de mérito", 
                    detalle = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Obtiene las promociones disponibles
        /// </summary>
        [HttpGet("promociones")]
        public async Task<ActionResult<IEnumerable<string>>> GetPromociones()
        {
            try
            {
                var promociones = await _context.Estudiantes
                    .Where(e => e.Promocion != null && e.Estado == "Activo")
                    .Select(e => e.Promocion!)
                    .Distinct()
                    .OrderByDescending(p => p)
                    .ToListAsync();

                return Ok(promociones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener promociones", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene la posición del estudiante actual en el orden de mérito
        /// </summary>
        [HttpGet("mi-posicion-merito")]
        public async Task<ActionResult<OrdenMeritoDto>> GetMiPosicionMerito()
        {
            try
            {
                var usuarioId = GetUsuarioId();
                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                // Verificar datos del estudiante
                if (string.IsNullOrEmpty(estudiante.Promocion))
                    return NotFound(new { 
                        mensaje = "Tu promoción no ha sido registrada. Contacta con administración.",
                        codigo = estudiante.Codigo,
                        requiereActualizacion = true
                    });

                var query = $"SELECT * FROM vw_OrdenMerito WHERE codigo = '{estudiante.Codigo}'";

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = query;

                OrdenMeritoDto? miPosicion = null;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        miPosicion = new OrdenMeritoDto
                        {
                            Posicion = Convert.ToInt32(reader.GetInt64(reader.GetOrdinal("posicion"))),
                            Codigo = reader.GetString(reader.GetOrdinal("codigo")),
                            Nombres = reader.GetString(reader.GetOrdinal("nombres")),
                            Apellidos = reader.GetString(reader.GetOrdinal("apellidos")),
                            Promocion = reader.IsDBNull(reader.GetOrdinal("promocion")) ? "" : reader.GetString(reader.GetOrdinal("promocion")),
                            Semestre = reader.GetInt32(reader.GetOrdinal("ciclo_actual")),
                            CreditosLlevadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_llevados_semestre")),
                            CreditosAprobadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_aprobados_semestre")),
                            TotalCreditosLlevados = reader.GetInt32(reader.GetOrdinal("total_creditos_llevados")),
                            TotalCreditosAprobados = reader.GetInt32(reader.GetOrdinal("total_creditos_aprobados")),
                            PromedioPonderadoSemestral = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_semestral")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_semestral")),
                            PromedioPonderadoAcumulado = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_acumulado")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_acumulado")),
                            RangoMerito = reader.IsDBNull(reader.GetOrdinal("rango_merito")) ? "" : reader.GetString(reader.GetOrdinal("rango_merito")),
                            TotalEstudiantes = reader.GetInt32(reader.GetOrdinal("total_estudiantes")),
                            PeriodoNombre = reader.IsDBNull(reader.GetOrdinal("periodo_nombre")) ? null : reader.GetString(reader.GetOrdinal("periodo_nombre")),
                            EstadoPeriodo = reader.IsDBNull(reader.GetOrdinal("estado_periodo")) ? null : reader.GetString(reader.GetOrdinal("estado_periodo"))
                        };
                    }
                }

                await connection.CloseAsync();

                if (miPosicion == null)
                    return NotFound(new { 
                        mensaje = "No apareces en el orden de mérito. Verifica que tu estado sea 'Activo' y tengas promoción asignada.",
                        codigo = estudiante.Codigo,
                        promocion = estudiante.Promocion,
                        estado = estudiante.Estado
                    });

                return Ok(miPosicion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    mensaje = "Error al obtener posición", 
                    detalle = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("cambiar-contrasena")]
        public async Task<ActionResult> CambiarContrasena([FromBody] CambiarContrasenaDto request)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                
                // Buscar el usuario
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                // Verificar la contraseña actual usando BCrypt
                if (!BCrypt.Net.BCrypt.Verify(request.ContrasenaActual, usuario.PasswordHash))
                    return BadRequest(new { mensaje = "La contraseña actual es incorrecta" });

                // Validar que la nueva contraseña tenga al menos 6 caracteres
                if (string.IsNullOrWhiteSpace(request.ContrasenaNueva) || request.ContrasenaNueva.Length < 6)
                    return BadRequest(new { mensaje = "La nueva contraseña debe tener al menos 6 caracteres" });

                // Hashear y actualizar la contraseña
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.ContrasenaNueva);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al cambiar contraseña", detalle = ex.Message });
            }
        }

        [HttpPut("actualizar-perfil")]
        public async Task<ActionResult<EstudianteDto>> ActualizarPerfil([FromBody] ActualizarPerfilDto request)
        {
            try
            {
                var usuarioId = GetUsuarioId();
                
                // Buscar el estudiante
                var estudiante = await _context.Estudiantes
                    .Include(e => e.Usuario)
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                    return NotFound(new { mensaje = "Estudiante no encontrado" });

                // Actualizar campos
                estudiante.Apellidos = request.Apellidos;
                estudiante.Nombres = request.Nombres;
                estudiante.Dni = request.Dni;
                estudiante.FechaNacimiento = request.FechaNacimiento;
                estudiante.Correo = request.Correo;
                estudiante.Telefono = request.Telefono;
                estudiante.Direccion = request.Direccion;

                await _context.SaveChangesAsync();

                // Retornar el perfil actualizado
                var perfilActualizado = await _estudianteService.GetByUsuarioIdAsync(usuarioId);
                return Ok(new { 
                    mensaje = "Perfil actualizado exitosamente", 
                    estudiante = perfilActualizado 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar perfil", detalle = ex.Message });
            }
        }
    }
}
