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
    [Route("api/trabajos-estudiante")]
    public class TrabajosEstudianteController : ControllerBase
    {
        private readonly ITrabajoService _trabajoService;
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<TrabajosEstudianteController> _logger;
        private readonly string _uploadsPath;

        public TrabajosEstudianteController(
            ITrabajoService trabajoService,
            GestionAcademicaContext context,
            ILogger<TrabajosEstudianteController> logger)
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
        // ENDPOINTS PARA ESTUDIANTES
        // ============================================

        /// <summary>
        /// Endpoint de prueba para verificar que el controlador funciona
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new { message = "TrabajosEstudianteController está funcionando", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Obtener trabajos disponibles para el estudiante
        /// </summary>
        [HttpGet("disponibles")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<List<TrabajoSimpleDto>>> GetTrabajosDisponibles()
        {
            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized("No se pudo identificar al usuario");

            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null)
                return NotFound("Estudiante no encontrado");

            var trabajos = await _trabajoService.GetTrabajosDisponiblesAsync(estudiante.Id);
            return Ok(trabajos);
        }

        /// <summary>
        /// Obtener trabajos por curso
        /// </summary>
        [HttpGet("curso/{idCurso}")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<List<TrabajoSimpleDto>>> GetTrabajosPorCurso(int idCurso)
        {
            try
            {
                _logger.LogInformation($"GetTrabajosPorCurso llamado - idCurso: {idCurso}");

                if (!User.TryGetUserId(out int usuarioId))
                {
                    _logger.LogWarning("No se pudo obtener el userId del token");
                    return Unauthorized("No se pudo identificar al usuario");
                }

                _logger.LogInformation($"UsuarioId obtenido: {usuarioId}");

                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                {
                    _logger.LogWarning($"Estudiante no encontrado para usuarioId: {usuarioId}");
                    return NotFound("Estudiante no encontrado");
                }

                _logger.LogInformation($"Estudiante encontrado - Id: {estudiante.Id}");

                // Verificar matrícula para debugging
                var matriculas = await _context.Matriculas
                    .Where(m => m.IdEstudiante == estudiante.Id && m.IdCurso == idCurso)
                    .ToListAsync();
                _logger.LogInformation($"Matrículas encontradas para estudiante {estudiante.Id} en curso {idCurso}: {matriculas.Count}");
                foreach (var mat in matriculas)
                {
                    _logger.LogInformation($"  - Matrícula ID: {mat.Id}, Estado: {mat.Estado}, Período: {mat.IdPeriodo}");
                }

                // Verificar trabajos en el curso
                var trabajosEnCurso = await _context.Set<TrabajoEncargado>()
                    .Where(t => t.IdCurso == idCurso)
                    .ToListAsync();
                _logger.LogInformation($"Trabajos totales en curso {idCurso}: {trabajosEnCurso.Count}");
                foreach (var t in trabajosEnCurso)
                {
                    _logger.LogInformation($"  - Trabajo ID: {t.Id}, Título: {t.Titulo}, Activo: {t.Activo}");
                }

                var trabajos = await _trabajoService.GetTrabajosPorCursoEstudianteAsync(idCurso, estudiante.Id);
                _logger.LogInformation($"Trabajos retornados para estudiante: {trabajos.Count}");

                return Ok(trabajos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en GetTrabajosPorCurso - idCurso: {idCurso}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Obtener un trabajo específico
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<TrabajoDto>> GetTrabajo(int id)
        {
            try
            {
                _logger.LogInformation($"GetTrabajo llamado - id: {id}");

                if (!User.TryGetUserId(out int usuarioId))
                {
                    _logger.LogWarning("No se pudo obtener el userId del token");
                    return Unauthorized("No se pudo identificar al usuario");
                }

                _logger.LogInformation($"UsuarioId obtenido: {usuarioId}");

                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                {
                    _logger.LogWarning($"Estudiante no encontrado para usuarioId: {usuarioId}");
                    return NotFound("Estudiante no encontrado");
                }

                _logger.LogInformation($"Estudiante encontrado - Id: {estudiante.Id}");

                // Verificar si el trabajo existe
                var trabajoExiste = await _context.Set<TrabajoEncargado>()
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (trabajoExiste == null)
                {
                    _logger.LogWarning($"Trabajo con ID {id} no existe");
                    return NotFound($"Trabajo con ID {id} no encontrado");
                }

                _logger.LogInformation($"Trabajo encontrado - Id: {trabajoExiste.Id}, Curso: {trabajoExiste.IdCurso}, Activo: {trabajoExiste.Activo}");

                // Verificar matrícula
                var matriculas = await _context.Matriculas
                    .Where(m => m.IdEstudiante == estudiante.Id && m.IdCurso == trabajoExiste.IdCurso)
                    .ToListAsync();
                _logger.LogInformation($"Matrículas encontradas para estudiante {estudiante.Id} en curso {trabajoExiste.IdCurso}: {matriculas.Count}");

                var trabajo = await _trabajoService.GetTrabajoParaEstudianteAsync(id, estudiante.Id);
                
                if (trabajo == null)
                {
                    _logger.LogWarning($"GetTrabajoParaEstudianteAsync retornó null para trabajo {id} y estudiante {estudiante.Id}");
                    return NotFound($"Trabajo con ID {id} no encontrado o no tienes acceso");
                }

                _logger.LogInformation($"Trabajo retornado exitosamente - Id: {trabajo.Id}, Título: {trabajo.Titulo}");
                return Ok(trabajo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en GetTrabajo - id: {id}");
                return StatusCode(500, new { message = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crear una entrega de trabajo
        /// </summary>
        [HttpPost("entregas")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<EntregaDto>> CreateEntrega([FromForm] EntregaCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized("No se pudo identificar al usuario");

            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null)
                return NotFound("Estudiante no encontrado");

            // Procesar archivos si existen
            if (Request.Form.Files != null && Request.Form.Files.Count > 0)
            {
                dto.Archivos = new List<ArchivoDto>();
                var entregaDir = Path.Combine(_uploadsPath, "Entregas");

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(entregaDir, fileName);
                        
                        Directory.CreateDirectory(entregaDir);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        dto.Archivos.Add(new ArchivoDto
                        {
                            NombreArchivo = file.FileName,
                            RutaArchivo = filePath,
                            TipoArchivo = file.ContentType?.Length > 100 ? file.ContentType.Substring(0, 100) : file.ContentType,
                            Tamaño = file.Length,
                            FechaSubida = DateTime.UtcNow
                        });
                    }
                }
            }

            var (success, error, created) = await _trabajoService.CrearEntregaAsync(dto, estudiante.Id);
            
            if (!success)
                return BadRequest(error);

            // Guardar archivos en BD
            if (dto.Archivos != null && created != null)
            {
                foreach (var archivoDto in dto.Archivos)
                {
                    var archivo = new TrabajoEntregaArchivo
                    {
                        IdEntrega = created.Id,
                        NombreArchivo = archivoDto.NombreArchivo,
                        RutaArchivo = archivoDto.RutaArchivo,
                        TipoArchivo = archivoDto.TipoArchivo?.Length > 100 ? archivoDto.TipoArchivo.Substring(0, 100) : archivoDto.TipoArchivo,
                        Tamaño = archivoDto.Tamaño,
                        FechaSubida = archivoDto.FechaSubida
                    };
                    _context.Set<TrabajoEntregaArchivo>().Add(archivo);
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetEntrega), new { id = created!.Id }, created);
        }

        /// <summary>
        /// Actualizar una entrega
        /// </summary>
        [HttpPut("entregas/{id}")]
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> UpdateEntrega(int id, [FromForm] EntregaUpdateDto dto)
        {
            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized("No se pudo identificar al usuario");

            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null)
                return NotFound("Estudiante no encontrado");

            // Procesar nuevos archivos si existen
            if (Request.Form.Files != null && Request.Form.Files.Count > 0)
            {
                dto.ArchivosNuevos = new List<ArchivoDto>();
                var entregaDir = Path.Combine(_uploadsPath, "Entregas");

                foreach (var file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(entregaDir, fileName);
                        
                        Directory.CreateDirectory(entregaDir);
                        
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

            var (notFound, success, error) = await _trabajoService.ActualizarEntregaAsync(id, dto, estudiante.Id);
            
            if (notFound)
                return NotFound($"Entrega con ID {id} no encontrada");
            
            if (!success)
                return BadRequest(error);

            // Guardar nuevos archivos en BD
            if (dto.ArchivosNuevos != null)
            {
                foreach (var archivoDto in dto.ArchivosNuevos)
                {
                    var archivo = new TrabajoEntregaArchivo
                    {
                        IdEntrega = id,
                        NombreArchivo = archivoDto.NombreArchivo,
                        RutaArchivo = archivoDto.RutaArchivo,
                        TipoArchivo = archivoDto.TipoArchivo,
                        Tamaño = archivoDto.Tamaño,
                        FechaSubida = archivoDto.FechaSubida
                    };
                    _context.Set<TrabajoEntregaArchivo>().Add(archivo);
                }
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        /// <summary>
        /// Obtener una entrega específica
        /// </summary>
        [HttpGet("entregas/{id}")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<EntregaDto>> GetEntrega(int id)
        {
            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized("No se pudo identificar al usuario");

            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null)
                return NotFound("Estudiante no encontrado");

            var entrega = await _trabajoService.GetEntregaAsync(id);
            
            if (entrega == null)
                return NotFound($"Entrega con ID {id} no encontrada");

            // Verificar que la entrega pertenece al estudiante
            if (entrega.IdEstudiante != estudiante.Id)
                return Forbid("No tienes permiso para ver esta entrega");

            return Ok(entrega);
        }

        /// <summary>
        /// Obtener la entrega del estudiante para un trabajo específico
        /// </summary>
        [HttpGet("trabajos/{idTrabajo}/mi-entrega")]
        [Authorize(Roles = "Estudiante")]
        public async Task<ActionResult<EntregaDto>> GetMiEntrega(int idTrabajo)
        {
            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized("No se pudo identificar al usuario");

            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null)
                return NotFound("Estudiante no encontrado");

            var entrega = await _context.Set<TrabajoEntrega>()
                .FirstOrDefaultAsync(e => e.IdTrabajo == idTrabajo && e.IdEstudiante == estudiante.Id);

            if (entrega == null)
                return NotFound("No se encontró una entrega para este trabajo");

            var entregaDto = await _trabajoService.GetEntregaAsync(entrega.Id);
            return Ok(entregaDto);
        }

        /// <summary>
        /// Descargar archivo de instrucciones
        /// </summary>
        [HttpGet("archivos/{idArchivo}/download")]
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> DownloadArchivoInstrucciones(int idArchivo)
        {
            try
            {
                _logger.LogInformation($"DownloadArchivoInstrucciones llamado - idArchivo: {idArchivo}");

                var archivo = await _context.Set<TrabajoArchivo>()
                    .Include(a => a.Trabajo)
                    .FirstOrDefaultAsync(a => a.Id == idArchivo);

                if (archivo == null)
                {
                    _logger.LogWarning($"Archivo con ID {idArchivo} no encontrado en la base de datos");
                    return NotFound("Archivo no encontrado");
                }

                if (!System.IO.File.Exists(archivo.RutaArchivo))
                {
                    _logger.LogWarning($"Archivo físico no encontrado en ruta: {archivo.RutaArchivo}");
                    return NotFound("El archivo físico no existe en el servidor");
                }

                // Verificar que el estudiante tiene acceso al trabajo
                if (!User.TryGetUserId(out int usuarioId))
                {
                    _logger.LogWarning("No se pudo obtener el userId del token");
                    return Unauthorized("No se pudo identificar al usuario");
                }

                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null)
                {
                    _logger.LogWarning($"Estudiante no encontrado para usuarioId: {usuarioId}");
                    return NotFound("Estudiante no encontrado");
                }

                // Verificación más flexible: cualquier matrícula que no esté retirada
                var tieneAcceso = await _context.Matriculas
                    .AnyAsync(m => m.IdEstudiante == estudiante.Id 
                        && m.IdCurso == archivo.Trabajo!.IdCurso 
                        && m.Estado != "Retirado");

                if (!tieneAcceso)
                {
                    _logger.LogWarning($"Estudiante {estudiante.Id} no tiene acceso al curso {archivo.Trabajo.IdCurso}");
                    return Forbid("No tienes acceso a este archivo");
                }

                _logger.LogInformation($"Descargando archivo: {archivo.NombreArchivo} desde {archivo.RutaArchivo}");
                var fileBytes = await System.IO.File.ReadAllBytesAsync(archivo.RutaArchivo);
                return File(fileBytes, archivo.TipoArchivo ?? "application/octet-stream", archivo.NombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al descargar archivo {idArchivo}");
                return StatusCode(500, new { message = "Error interno del servidor al descargar el archivo", detail = ex.Message });
            }
        }

        /// <summary>
        /// Descargar archivo de entrega
        /// </summary>
        [HttpGet("entregas/{idEntrega}/archivos/{idArchivo}/download")]
        [Authorize(Roles = "Estudiante,Docente,Administrador")]
        public async Task<IActionResult> DownloadArchivoEntrega(int idEntrega, int idArchivo)
        {
            var archivo = await _context.Set<TrabajoEntregaArchivo>()
                .Include(a => a.Entrega)
                .FirstOrDefaultAsync(a => a.Id == idArchivo && a.IdEntrega == idEntrega);

            if (archivo == null || !System.IO.File.Exists(archivo.RutaArchivo))
                return NotFound("Archivo no encontrado");

            // Verificar permisos
            if (!User.TryGetUserId(out int usuarioId))
                return Unauthorized();

            var esEstudiante = User.IsInRole("Estudiante");
            var esDocente = User.IsInRole("Docente") || User.IsInRole("Administrador");

            if (esEstudiante)
            {
                var estudiante = await _context.Estudiantes
                    .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

                if (estudiante == null || archivo.Entrega!.IdEstudiante != estudiante.Id)
                    return Forbid("No tienes permiso para ver este archivo");
            }
            else if (esDocente)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                int? docenteId = null;
                
                if (role == "Docente")
                {
                    if (!User.TryGetDocenteId(out int docenteIdValue))
                        return Unauthorized("No se pudo identificar al docente");
                    docenteId = docenteIdValue;
                }
                else if (role == "Administrador")
                {
                    // Si es administrador, puede ver cualquier archivo de entrega
                    docenteId = null; // null significa que puede verlo sin restricción
                }

                if (docenteId.HasValue)
                {
                    var trabajo = await _context.Set<TrabajoEncargado>()
                        .FirstOrDefaultAsync(t => t.Id == archivo.Entrega!.IdTrabajo);

                    if (trabajo == null || trabajo.IdDocente != docenteId.Value)
                        return Forbid("No tienes permiso para ver este archivo");
                }
                // Si es administrador (docenteId es null), puede ver el archivo sin verificación adicional
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(archivo.RutaArchivo);
            return File(fileBytes, archivo.TipoArchivo ?? "application/octet-stream", archivo.NombreArchivo);
        }

    }
}

