using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using API_REST_CURSOSACADEMICOS.Extensions;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrabajosController : ControllerBase
    {
        private readonly ITrabajoService _trabajoService;
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<TrabajosController> _logger;
        private readonly string _uploadsPath;

        public TrabajosController(
            ITrabajoService trabajoService,
            GestionAcademicaContext context,
            ILogger<TrabajosController> logger)
        {
            _trabajoService = trabajoService;
            _context = context;
            _logger = logger;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Trabajos");
            
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        // ============================================
        // ENDPOINTS PARA DOCENTES
        // ============================================

        /// <summary>
        /// Endpoint temporal para debug - ver claims del usuario
        /// </summary>
        [HttpGet("debug/claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var docenteId = User.FindFirst("DocenteId")?.Value;
            var nameId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return Ok(new
            {
                claims,
                role,
                docenteId,
                nameId,
                isInRoleDocente = User.IsInRole("Docente"),
                isInRoleAdmin = User.IsInRole("Administrador")
            });
        }

        /// <summary>
        /// Obtener trabajos por curso
        /// </summary>
        [HttpGet("curso/{idCurso}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<List<TrabajoDto>>> GetTrabajosPorCurso(int idCurso)
        {
            try
            {
                // Log para debugging
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"GetTrabajosPorCurso - Usuario: {userId}, Rol: {role}, CursoId: {idCurso}");

                // Este endpoint no requiere docenteId porque el servicio no filtra por docente
                var trabajos = await _trabajoService.GetTrabajosPorCursoAsync(idCurso);
                return Ok(trabajos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener trabajos del curso {idCurso}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Obtener trabajos del docente
        /// </summary>
        [HttpGet("docente")]
        [Authorize(Roles = "Docente")]
        public async Task<ActionResult<List<TrabajoDto>>> GetTrabajosPorDocente()
        {
            if (!User.TryGetDocenteId(out int docenteId))
                return Unauthorized("No se pudo identificar al docente");

            var trabajos = await _trabajoService.GetTrabajosPorDocenteAsync(docenteId);
            return Ok(trabajos);
        }

        /// <summary>
        /// Obtener un trabajo específico
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<TrabajoDto>> GetTrabajo(int id)
        {
            var trabajo = await _trabajoService.GetTrabajoAsync(id);
            if (trabajo == null)
                return NotFound($"Trabajo con ID {id} no encontrado");

            return Ok(trabajo);
        }

        /// <summary>
        /// Crear un nuevo trabajo encargado
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<TrabajoDto>> CreateTrabajo([FromForm] TrabajoCreateDto dto)
        {
            try
            {
                // Log para debugging
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var docenteIdClaim = User.FindFirst("DocenteId")?.Value;
                
                _logger.LogInformation($"CreateTrabajo - Usuario: {userId}, Email: {email}, Rol: {role}, DocenteId claim: {docenteIdClaim}, IdCurso: {dto.IdCurso}");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning($"ModelState inválido: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)))}");
                    return BadRequest(ModelState);
                }

                int docenteId;
                
                if (role == "Docente")
                {
                    if (!User.TryGetDocenteId(out docenteId))
                    {
                        _logger.LogWarning($"No se pudo obtener DocenteId del token. Claims disponibles: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                        return Unauthorized("No se pudo identificar al docente");
                    }
                    _logger.LogInformation($"DocenteId obtenido del token: {docenteId}");
                }
                else if (role == "Administrador")
                {
                    // Si es administrador, obtener el docenteId del curso
                    var curso = await _context.Set<Curso>()
                        .FirstOrDefaultAsync(c => c.Id == dto.IdCurso);
                    
                    if (curso == null)
                    {
                        _logger.LogWarning($"Curso con ID {dto.IdCurso} no encontrado");
                        return NotFound($"Curso con ID {dto.IdCurso} no encontrado");
                    }
                    
                    if (curso.IdDocente == null)
                    {
                        _logger.LogWarning($"Curso {dto.IdCurso} no tiene docente asignado");
                        return BadRequest("El curso no tiene un docente asignado");
                    }
                    
                    docenteId = curso.IdDocente.Value;
                    _logger.LogInformation($"DocenteId obtenido del curso: {docenteId}");
                }
                else
                {
                    _logger.LogWarning($"Rol no permitido: {role}");
                    return Forbid();
                }

            // Procesar archivos si existen
            if (Request.Form.Files != null && Request.Form.Files.Count > 0)
            {
                dto.Archivos = new List<ArchivoDto>();
                var trabajoDir = Path.Combine(_uploadsPath, "Instrucciones");

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(trabajoDir, fileName);
                        
                        Directory.CreateDirectory(trabajoDir);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        dto.Archivos.Add(new ArchivoDto
                        {
                            NombreArchivo = file.FileName,
                            RutaArchivo = filePath,
                            TipoArchivo = file.ContentType,
                            Tamaño = file.Length,
                            FechaSubida = DateTime.UtcNow
                        });
                    }
                }
            }

            // Procesar FormData para campos adicionales
            if (Request.Form.ContainsKey("IdTipoEvaluacion") && int.TryParse(Request.Form["IdTipoEvaluacion"].ToString(), out int idTipoEvaluacion))
            {
                dto.IdTipoEvaluacion = idTipoEvaluacion;
            }

            if (Request.Form.ContainsKey("NumeroTrabajo") && int.TryParse(Request.Form["NumeroTrabajo"].ToString(), out int numeroTrabajo))
            {
                dto.NumeroTrabajo = numeroTrabajo;
            }

            if (Request.Form.ContainsKey("TotalTrabajos") && int.TryParse(Request.Form["TotalTrabajos"].ToString(), out int totalTrabajos))
            {
                dto.TotalTrabajos = totalTrabajos;
            }

                var (success, error, created) = await _trabajoService.CreateTrabajoAsync(dto, docenteId);
                
                if (!success)
                {
                    _logger.LogWarning($"Error al crear trabajo: {error}");
                    return BadRequest(error);
                }

                // Guardar archivos en BD
                if (dto.Archivos != null && created != null)
                {
                    foreach (var archivoDto in dto.Archivos)
                    {
                        var archivo = new TrabajoArchivo
                        {
                            IdTrabajo = created.Id,
                            NombreArchivo = archivoDto.NombreArchivo,
                            RutaArchivo = archivoDto.RutaArchivo,
                            TipoArchivo = archivoDto.TipoArchivo,
                            Tamaño = archivoDto.Tamaño,
                            FechaSubida = archivoDto.FechaSubida
                        };
                        _context.Set<TrabajoArchivo>().Add(archivo);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Trabajo creado exitosamente con ID: {created!.Id}");
                return CreatedAtAction(nameof(GetTrabajo), new { id = created!.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear trabajo. DTO: IdCurso={dto.IdCurso}, Titulo={dto.Titulo}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar un trabajo
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> UpdateTrabajo(int id, [FromForm] TrabajoUpdateDto dto)
        {
            if (!User.TryGetDocenteId(out int docenteId))
                return Unauthorized("No se pudo identificar al docente");

            // Procesar nuevos archivos si existen
            if (Request.Form.Files != null && Request.Form.Files.Count > 0)
            {
                dto.ArchivosNuevos = new List<ArchivoDto>();
                var trabajoDir = Path.Combine(_uploadsPath, "Instrucciones");

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(trabajoDir, fileName);
                        
                        Directory.CreateDirectory(trabajoDir);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        dto.ArchivosNuevos.Add(new ArchivoDto
                        {
                            NombreArchivo = file.FileName,
                            RutaArchivo = filePath,
                            TipoArchivo = file.ContentType,
                            Tamaño = file.Length,
                            FechaSubida = DateTime.UtcNow
                        });
                    }
                }
            }

            // Procesar FormData para campos adicionales
            if (Request.Form.ContainsKey("IdTipoEvaluacion") && int.TryParse(Request.Form["IdTipoEvaluacion"].ToString(), out int idTipoEvaluacion))
            {
                dto.IdTipoEvaluacion = idTipoEvaluacion;
            }

            if (Request.Form.ContainsKey("NumeroTrabajo") && int.TryParse(Request.Form["NumeroTrabajo"].ToString(), out int numeroTrabajo))
            {
                dto.NumeroTrabajo = numeroTrabajo;
            }

            if (Request.Form.ContainsKey("TotalTrabajos") && int.TryParse(Request.Form["TotalTrabajos"].ToString(), out int totalTrabajos))
            {
                dto.TotalTrabajos = totalTrabajos;
            }

            var (notFound, success, error) = await _trabajoService.UpdateTrabajoAsync(id, dto, docenteId);
            
            if (notFound)
                return NotFound($"Trabajo con ID {id} no encontrado");
            
            if (!success)
                return BadRequest(error);

            // Guardar nuevos archivos en BD
            if (dto.ArchivosNuevos != null)
            {
                foreach (var archivoDto in dto.ArchivosNuevos)
                {
                    var archivo = new TrabajoArchivo
                    {
                        IdTrabajo = id,
                        NombreArchivo = archivoDto.NombreArchivo,
                        RutaArchivo = archivoDto.RutaArchivo,
                        TipoArchivo = archivoDto.TipoArchivo,
                        Tamaño = archivoDto.Tamaño,
                        FechaSubida = archivoDto.FechaSubida
                    };
                    _context.Set<TrabajoArchivo>().Add(archivo);
                }
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        /// <summary>
        /// Eliminar un trabajo
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> DeleteTrabajo(int id)
        {
            if (!User.TryGetDocenteId(out int docenteId))
                return Unauthorized("No se pudo identificar al docente");

            var (notFound, success, error) = await _trabajoService.DeleteTrabajoAsync(id, docenteId);
            
            if (notFound)
                return NotFound($"Trabajo con ID {id} no encontrado");
            
            if (!success)
                return BadRequest(error);

            return NoContent();
        }

        /// <summary>
        /// Obtener entregas de un trabajo
        /// </summary>
        [HttpGet("{id}/entregas")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<List<EntregaDto>>> GetEntregasPorTrabajo(int id)
        {
            // Si es administrador, puede ver todas las entregas sin filtrar por docente
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? docenteId = null;
            
            if (role == "Docente")
            {
                if (!User.TryGetDocenteId(out int docenteIdValue))
                    return Unauthorized("No se pudo identificar al docente");
                docenteId = docenteIdValue;
            }

            // Si es administrador, docenteId será null y el servicio debería retornar todas las entregas
            // Necesitamos modificar el servicio o crear un método alternativo
            // Por ahora, si es administrador, usamos 0 para indicar "todos"
            var entregas = await _trabajoService.GetEntregasPorTrabajoAsync(id, docenteId ?? 0);
            return Ok(entregas);
        }

        /// <summary>
        /// Calificar una entrega
        /// </summary>
        [HttpPost("entregas/{idEntrega}/calificar")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> CalificarEntrega(int idEntrega, [FromBody] CalificarEntregaDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                int docenteId;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (role == "Docente")
                {
                    if (!User.TryGetDocenteId(out docenteId))
                    {
                        _logger.LogWarning("No se pudo obtener DocenteId del token para docente");
                        return Unauthorized("No se pudo identificar al docente");
                    }
                }
                else if (role == "Administrador")
                {
                    // Si es administrador, obtener el docenteId del trabajo asociado a la entrega
                    var entrega = await _context.Set<TrabajoEntrega>()
                        .Include(e => e.Trabajo)
                        .FirstOrDefaultAsync(e => e.Id == idEntrega);
                    
                    if (entrega == null || entrega.Trabajo == null)
                        return NotFound($"Entrega con ID {idEntrega} no encontrada");
                    
                    // IdDocente es int (no nullable), así que siempre tiene un valor
                    docenteId = entrega.Trabajo.IdDocente;
                    _logger.LogInformation($"Administrador calificando entrega {idEntrega} - DocenteId obtenido del trabajo: {docenteId}");
                }
                else
                {
                    return Forbid();
                }

                var (notFound, success, error) = await _trabajoService.CalificarEntregaAsync(idEntrega, dto, docenteId);
                
                if (notFound)
                    return NotFound($"Entrega con ID {idEntrega} no encontrada");
                
                if (!success)
                {
                    _logger.LogWarning($"Error al calificar entrega {idEntrega}: {error}");
                    return BadRequest(error);
                }

                _logger.LogInformation($"Entrega {idEntrega} calificada exitosamente por docente {docenteId}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al calificar entrega {idEntrega}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Descargar archivo de instrucciones
        /// </summary>
        [HttpGet("archivos/{idArchivo}/download")]
        [Authorize(Roles = "Docente,Administrador,Estudiante")]
        public async Task<IActionResult> DownloadArchivoInstrucciones(int idArchivo)
        {
            var archivo = await _context.Set<TrabajoArchivo>()
                .FirstOrDefaultAsync(a => a.Id == idArchivo);

            if (archivo == null || !System.IO.File.Exists(archivo.RutaArchivo))
                return NotFound("Archivo no encontrado");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(archivo.RutaArchivo);
            return File(fileBytes, archivo.TipoArchivo ?? "application/octet-stream", archivo.NombreArchivo);
        }

        /// <summary>
        /// Descargar archivo de entrega (para docentes)
        /// </summary>
        [HttpGet("entregas/{idEntrega}/archivos/{idArchivo}/download")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> DownloadArchivoEntrega(int idEntrega, int idArchivo)
        {
            try
            {
                var archivo = await _context.Set<TrabajoEntregaArchivo>()
                    .Include(a => a.Entrega)
                    .ThenInclude(e => e!.Trabajo)
                    .FirstOrDefaultAsync(a => a.Id == idArchivo && a.IdEntrega == idEntrega);

                if (archivo == null || !System.IO.File.Exists(archivo.RutaArchivo))
                    return NotFound("Archivo no encontrado");

                // Verificar permisos
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (role == "Docente")
                {
                    if (!User.TryGetDocenteId(out int docenteId))
                        return Unauthorized("No se pudo identificar al docente");

                    if (archivo.Entrega?.Trabajo == null || archivo.Entrega.Trabajo.IdDocente != docenteId)
                        return Forbid("No tienes permiso para ver este archivo");
                }
                // Si es administrador, puede ver cualquier archivo sin restricción

                var fileBytes = await System.IO.File.ReadAllBytesAsync(archivo.RutaArchivo);
                return File(fileBytes, archivo.TipoArchivo ?? "application/octet-stream", archivo.NombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al descargar archivo de entrega {idArchivo}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }
    }
}

