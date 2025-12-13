using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly IEstudianteService _estudianteService;
        private readonly IEstudiantesControllerService _controllerService;

        public EstudiantesController(IEstudianteService estudianteService, IEstudiantesControllerService controllerService)
        {
            _estudianteService = estudianteService;
            _controllerService = controllerService;
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
                var estudiantes = await _controllerService.GetAllAdminAsync();

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
                var estudiante = await _controllerService.GetByIdAdminAsync(id);

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
                var cursosMatriculados = await _controllerService.GetCursosMatriculadosIdsAsync(estudiante.Id, idPeriodo.Value);

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
                    creditosAcumulados = estudianteDto.CreditosAcumulados,
                    promedioAcumulado = estudianteDto.PromedioAcumulado,
                    promedioSemestral = estudianteDto.PromedioSemestral
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
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

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
                var estudiante = await _estudianteService.GetByUsuarioIdAsync(usuarioId);

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

                var payload = await _controllerService.VerificarPrerequisitosAsync(estudiante.Id, idCurso);
                return Ok(payload);
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
                var ordenMerito = await _controllerService.GetOrdenMeritoAsync(promocion);
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
                var promociones = await _controllerService.GetPromocionesAsync();
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
                var outcome = await _controllerService.GetMiPosicionMeritoAsync(usuarioId);
                if (outcome.Status == ServiceOutcomeStatus.Ok) return Ok(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.NotFound) return NotFound(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.BadRequest) return BadRequest(outcome.Payload);
                return StatusCode(500, new { mensaje = "Error al obtener posición" });
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
                var outcome = await _controllerService.CambiarContrasenaAsync(usuarioId, request);
                if (outcome.Status == ServiceOutcomeStatus.Ok) return Ok(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.BadRequest) return BadRequest(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.NotFound) return NotFound(outcome.Payload);
                return StatusCode(500, new { mensaje = "Error al cambiar contraseña" });
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
                var outcome = await _controllerService.ActualizarPerfilAsync(usuarioId, request);
                if (outcome.Status == ServiceOutcomeStatus.Ok) return Ok(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.BadRequest) return BadRequest(outcome.Payload);
                if (outcome.Status == ServiceOutcomeStatus.NotFound) return NotFound(outcome.Payload);
                return StatusCode(500, new { mensaje = "Error al actualizar perfil" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar perfil", detalle = ex.Message });
            }
        }
    }
}
