using System.Text.Json;
using API_REST_CURSOSACADEMICOS.Application.Events;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Domain.Events;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API_REST_CURSOSACADEMICOS.Services
{
    public class TrabajoService : ITrabajoService
    {
        private readonly GestionAcademicaContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TrabajoService> _logger;
        private readonly string _uploadsPath;

        public TrabajoService(
            GestionAcademicaContext context,
            IServiceProvider serviceProvider,
            ILogger<TrabajoService> logger)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Trabajos");
            
            // Crear directorio si no existe
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        // ============================================
        // OPERACIONES PARA DOCENTES
        // ============================================

        public async Task<List<TrabajoDto>> GetTrabajosPorCursoAsync(int idCurso)
        {
            var trabajos = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Docente)
                .Include(t => t.TipoEvaluacion)
                .Include(t => t.Archivos)
                .Include(t => t.Links)
                .Include(t => t.Entregas)
                .Where(t => t.IdCurso == idCurso)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return trabajos.Select(MapToDto).ToList();
        }

        public async Task<List<TrabajoDto>> GetTrabajosPorDocenteAsync(int idDocente)
        {
            var trabajos = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Docente)
                .Include(t => t.TipoEvaluacion)
                .Include(t => t.Archivos)
                .Include(t => t.Links)
                .Include(t => t.Entregas)
                .Where(t => t.IdDocente == idDocente)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return trabajos.Select(MapToDto).ToList();
        }

        public async Task<TrabajoDto?> GetTrabajoAsync(int id)
        {
            var trabajo = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Docente)
                .Include(t => t.TipoEvaluacion)
                .Include(t => t.Archivos)
                .Include(t => t.Links)
                .Include(t => t.Entregas)
                .FirstOrDefaultAsync(t => t.Id == id);

            return trabajo != null ? MapToDto(trabajo) : null;
        }

        public async Task<(bool success, string? error, TrabajoDto? created)> CreateTrabajoAsync(
            TrabajoCreateDto dto, int idDocente)
        {
            try
            {
                // Validar que el curso existe y pertenece al docente
                var curso = await _context.Cursos
                    .FirstOrDefaultAsync(c => c.Id == dto.IdCurso && c.IdDocente == idDocente);

                if (curso == null)
                {
                    return (false, "El curso no existe o no pertenece a este docente", null);
                }

                // Validar fecha límite
                if (dto.FechaLimite <= DateTime.UtcNow)
                {
                    return (false, "La fecha límite debe ser futura", null);
                }

                // Validar tipo de evaluación si se proporciona
                TipoEvaluacion? tipoEvaluacion = null;
                if (dto.IdTipoEvaluacion.HasValue)
                {
                    tipoEvaluacion = await _context.TiposEvaluacion
                        .FirstOrDefaultAsync(t => t.Id == dto.IdTipoEvaluacion.Value && t.IdCurso == dto.IdCurso && t.Activo);
                    
                    if (tipoEvaluacion == null)
                    {
                        return (false, "El tipo de evaluación no existe o no está activo para este curso", null);
                    }

                    // Validar división de evaluación
                    if (dto.NumeroTrabajo.HasValue || dto.TotalTrabajos.HasValue)
                    {
                        if (!dto.NumeroTrabajo.HasValue || !dto.TotalTrabajos.HasValue)
                        {
                            return (false, "Si se divide una evaluación, debe especificar tanto el número de trabajo como el total de trabajos", null);
                        }

                        if (dto.NumeroTrabajo.Value < 1 || dto.NumeroTrabajo.Value > dto.TotalTrabajos.Value)
                        {
                            return (false, "El número de trabajo debe estar entre 1 y el total de trabajos", null);
                        }

                        if (dto.TotalTrabajos.Value < 1 || dto.TotalTrabajos.Value > 10)
                        {
                            return (false, "El total de trabajos debe estar entre 1 y 10", null);
                        }

                        // Verificar que no exista ya un trabajo con el mismo número para este tipo de evaluación
                        var trabajoExistente = await _context.Set<TrabajoEncargado>()
                            .FirstOrDefaultAsync(t => t.IdCurso == dto.IdCurso 
                                && t.IdTipoEvaluacion == dto.IdTipoEvaluacion.Value
                                && t.NumeroTrabajo == dto.NumeroTrabajo.Value
                                && t.Activo);

                        if (trabajoExistente != null)
                        {
                            return (false, $"Ya existe un trabajo número {dto.NumeroTrabajo.Value} para este tipo de evaluación", null);
                        }

                        // Verificar consistencia con otros trabajos del mismo tipo
                        var otrosTrabajos = await _context.Set<TrabajoEncargado>()
                            .Where(t => t.IdCurso == dto.IdCurso 
                                && t.IdTipoEvaluacion == dto.IdTipoEvaluacion.Value
                                && t.NumeroTrabajo.HasValue
                                && t.Activo)
                            .OrderBy(t => t.NumeroTrabajo)
                            .ToListAsync();

                        if (otrosTrabajos.Any())
                        {
                            var totalTrabajosExistente = otrosTrabajos.First().TotalTrabajos;
                            if (totalTrabajosExistente.HasValue && totalTrabajosExistente.Value != dto.TotalTrabajos.Value)
                            {
                                var numerosExistentes = string.Join(", ", otrosTrabajos.Select(t => t.NumeroTrabajo));
                                return (false, $"Ya existe una partición de este tipo de evaluación con {totalTrabajosExistente.Value} trabajos. Trabajos existentes: {numerosExistentes}. Debe usar el mismo total de trabajos.", null);
                            }

                            // Informar sobre trabajos existentes
                            var numerosDisponibles = Enumerable.Range(1, dto.TotalTrabajos.Value)
                                .Where(n => !otrosTrabajos.Any(t => t.NumeroTrabajo == n))
                                .ToList();
                            
                            if (numerosDisponibles.Count == 0)
                            {
                                return (false, $"Ya se han creado todos los {dto.TotalTrabajos.Value} trabajos para este tipo de evaluación. No se pueden crear más.", null);
                            }
                        }
                    }
                }

                var trabajo = new TrabajoEncargado
                {
                    IdCurso = dto.IdCurso,
                    IdDocente = idDocente,
                    Titulo = dto.Titulo,
                    Descripcion = dto.Descripcion,
                    FechaCreacion = DateTime.UtcNow,
                    FechaLimite = dto.FechaLimite,
                    Activo = true,
                    IdTipoEvaluacion = dto.IdTipoEvaluacion,
                    NumeroTrabajo = dto.NumeroTrabajo,
                    TotalTrabajos = dto.TotalTrabajos
                };

                // Calcular peso individual si se divide la evaluación
                if (tipoEvaluacion != null && dto.TotalTrabajos.HasValue && dto.TotalTrabajos.Value > 1)
                {
                    trabajo.PesoIndividual = tipoEvaluacion.Peso / dto.TotalTrabajos.Value;
                    _logger.LogInformation($"Trabajo dividido - Tipo: {tipoEvaluacion.Nombre}, Peso total: {tipoEvaluacion.Peso}%, Peso individual: {trabajo.PesoIndividual}%");
                }
                else if (tipoEvaluacion != null)
                {
                    trabajo.PesoIndividual = tipoEvaluacion.Peso;
                }

                _context.Set<TrabajoEncargado>().Add(trabajo);
                await _context.SaveChangesAsync();

                // Procesar archivos (se guardarán en el controlador)
                // Procesar links
                if (dto.Links != null && dto.Links.Any())
                {
                    foreach (var linkDto in dto.Links)
                    {
                        var link = new TrabajoLink
                        {
                            IdTrabajo = trabajo.Id,
                            Url = linkDto.Url,
                            Descripcion = linkDto.Descripcion,
                            FechaCreacion = DateTime.UtcNow
                        };
                        _context.Set<TrabajoLink>().Add(link);
                    }
                    await _context.SaveChangesAsync();
                }

                // Notificar a estudiantes del curso
                await NotificarEstudiantesAsync(trabajo.Id, dto.IdCurso, trabajo.Titulo);

                var trabajoDto = await GetTrabajoAsync(trabajo.Id);
                return (true, null, trabajoDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear trabajo");
                return (false, $"Error al crear trabajo: {ex.Message}", null);
            }
        }

        public async Task<(bool notFound, bool success, string? error)> UpdateTrabajoAsync(
            int id, TrabajoUpdateDto dto, int idDocente)
        {
            try
            {
                var trabajo = await _context.Set<TrabajoEncargado>()
                    .Include(t => t.Archivos)
                    .Include(t => t.Links)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IdDocente == idDocente);

                if (trabajo == null)
                {
                    return (true, false, null);
                }

                if (!string.IsNullOrEmpty(dto.Titulo))
                    trabajo.Titulo = dto.Titulo;

                if (dto.Descripcion != null)
                    trabajo.Descripcion = dto.Descripcion;

                if (dto.FechaLimite.HasValue)
                {
                    if (dto.FechaLimite.Value <= DateTime.UtcNow)
                        return (false, false, "La fecha límite debe ser futura");
                    trabajo.FechaLimite = dto.FechaLimite.Value;
                }

                if (dto.Activo.HasValue)
                    trabajo.Activo = dto.Activo.Value;

                // Actualizar tipo de evaluación si se proporciona
                TipoEvaluacion? tipoEvaluacion = null;
                if (dto.IdTipoEvaluacion.HasValue)
                {
                    tipoEvaluacion = await _context.TiposEvaluacion
                        .FirstOrDefaultAsync(t => t.Id == dto.IdTipoEvaluacion.Value && t.IdCurso == trabajo.IdCurso && t.Activo);
                    
                    if (tipoEvaluacion == null)
                    {
                        return (false, false, "El tipo de evaluación no existe o no está activo para este curso");
                    }
                    
                    trabajo.IdTipoEvaluacion = dto.IdTipoEvaluacion.Value;
                }
                else if (dto.IdTipoEvaluacion == null && trabajo.IdTipoEvaluacion.HasValue)
                {
                    // Si se envía null explícitamente, se puede eliminar la relación
                    // Pero por seguridad, solo permitimos actualizar, no eliminar si ya hay entregas calificadas
                    var tieneEntregasCalificadas = await _context.Set<TrabajoEntrega>()
                        .AnyAsync(e => e.IdTrabajo == id && e.Calificacion.HasValue);
                    
                    if (!tieneEntregasCalificadas)
                    {
                        trabajo.IdTipoEvaluacion = null;
                    trabajo.NumeroTrabajo = null;
                    trabajo.TotalTrabajos = null;
                    trabajo.PesoIndividual = null;
                    }
                }

                // Actualizar división de evaluación si se proporciona
                if (dto.NumeroTrabajo.HasValue || dto.TotalTrabajos.HasValue)
                {
                    if (!dto.NumeroTrabajo.HasValue || !dto.TotalTrabajos.HasValue)
                    {
                        return (false, false, "Si se divide una evaluación, debe especificar tanto el número de trabajo como el total de trabajos");
                    }

                    // Verificar que no haya entregas calificadas antes de cambiar la división
                    var tieneEntregasCalificadas = await _context.Set<TrabajoEntrega>()
                        .AnyAsync(e => e.IdTrabajo == id && e.Calificacion.HasValue);
                    
                    if (tieneEntregasCalificadas && (trabajo.NumeroTrabajo != dto.NumeroTrabajo || trabajo.TotalTrabajos != dto.TotalTrabajos))
                    {
                        return (false, false, "No se puede modificar la división de evaluación si ya hay entregas calificadas");
                    }

                    trabajo.NumeroTrabajo = dto.NumeroTrabajo.Value;
                    trabajo.TotalTrabajos = dto.TotalTrabajos.Value;

                    // Recalcular peso individual
                    if (trabajo.IdTipoEvaluacion.HasValue)
                    {
                        tipoEvaluacion ??= await _context.TiposEvaluacion
                            .FirstOrDefaultAsync(t => t.Id == trabajo.IdTipoEvaluacion.Value);
                        
                        if (tipoEvaluacion != null)
                        {
                            trabajo.PesoIndividual = tipoEvaluacion.Peso / dto.TotalTrabajos.Value;
                            
                            // Actualizar pesos de otros trabajos del mismo tipo
                            await RecalcularPesosTrabajosAsync(trabajo.IdCurso, trabajo.IdTipoEvaluacion.Value, dto.TotalTrabajos.Value);
                        }
                    }
                }

                trabajo.FechaActualizacion = DateTime.UtcNow;

                // Eliminar archivos
                if (dto.ArchivosEliminar != null && dto.ArchivosEliminar.Any())
                {
                    var archivosEliminar = await _context.Set<TrabajoArchivo>()
                        .Where(a => dto.ArchivosEliminar.Contains(a.Id) && a.IdTrabajo == id)
                        .ToListAsync();

                    foreach (var archivo in archivosEliminar)
                    {
                        // Eliminar archivo físico
                        if (File.Exists(archivo.RutaArchivo))
                        {
                            try { File.Delete(archivo.RutaArchivo); } catch { }
                        }
                        _context.Set<TrabajoArchivo>().Remove(archivo);
                    }
                }

                // Eliminar links
                if (dto.LinksEliminar != null && dto.LinksEliminar.Any())
                {
                    var linksEliminar = await _context.Set<TrabajoLink>()
                        .Where(l => dto.LinksEliminar.Contains(l.Id) && l.IdTrabajo == id)
                        .ToListAsync();

                    _context.Set<TrabajoLink>().RemoveRange(linksEliminar);
                }

                // Agregar nuevos links
                if (dto.LinksNuevos != null && dto.LinksNuevos.Any())
                {
                    foreach (var linkDto in dto.LinksNuevos)
                    {
                        var link = new TrabajoLink
                        {
                            IdTrabajo = trabajo.Id,
                            Url = linkDto.Url,
                            Descripcion = linkDto.Descripcion,
                            FechaCreacion = DateTime.UtcNow
                        };
                        _context.Set<TrabajoLink>().Add(link);
                    }
                }

                await _context.SaveChangesAsync();
                return (false, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar trabajo");
                return (false, false, $"Error al actualizar trabajo: {ex.Message}");
            }
        }

        public async Task<(bool notFound, bool success, string? error)> DeleteTrabajoAsync(int id, int idDocente)
        {
            try
            {
                var trabajo = await _context.Set<TrabajoEncargado>()
                    .Include(t => t.Archivos)
                    .Include(t => t.Entregas)
                        .ThenInclude(e => e.Archivos)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IdDocente == idDocente);

                if (trabajo == null)
                {
                    return (true, false, null);
                }

                // Eliminar archivos físicos
                foreach (var archivo in trabajo.Archivos)
                {
                    if (File.Exists(archivo.RutaArchivo))
                    {
                        try { File.Delete(archivo.RutaArchivo); } catch { }
                    }
                }

                foreach (var entrega in trabajo.Entregas)
                {
                    foreach (var archivo in entrega.Archivos)
                    {
                        if (File.Exists(archivo.RutaArchivo))
                        {
                            try { File.Delete(archivo.RutaArchivo); } catch { }
                        }
                    }
                }

                _context.Set<TrabajoEncargado>().Remove(trabajo);
                await _context.SaveChangesAsync();

                return (false, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar trabajo");
                return (false, false, $"Error al eliminar trabajo: {ex.Message}");
            }
        }

        // ============================================
        // OPERACIONES PARA ESTUDIANTES
        // ============================================

        public async Task<List<TrabajoSimpleDto>> GetTrabajosDisponiblesAsync(int idEstudiante)
        {
            // Obtener cursos del estudiante en el período activo
            var periodoActivo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.Activo);

            if (periodoActivo == null)
                return new List<TrabajoSimpleDto>();

            var matriculas = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.IdEstudiante == idEstudiante 
                    && m.IdPeriodo == periodoActivo.Id 
                    && m.Estado == "Activo")
                .Select(m => m.IdCurso)
                .ToListAsync();

            var trabajos = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Entregas)
                .Where(t => matriculas.Contains(t.IdCurso) && t.Activo)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return trabajos.Select(t => new TrabajoSimpleDto
            {
                Id = t.Id,
                IdCurso = t.IdCurso,
                NombreCurso = t.Curso?.NombreCurso,
                Titulo = t.Titulo,
                FechaCreacion = t.FechaCreacion,
                FechaLimite = t.FechaLimite,
                Activo = t.Activo,
                TotalEntregas = t.Entregas.Count,
                YaEntregado = t.Entregas.Any(e => e.IdEstudiante == idEstudiante)
            }).ToList();
        }

        public async Task<TrabajoDto?> GetTrabajoParaEstudianteAsync(int id, int idEstudiante)
        {
            // Obtener el trabajo
            var trabajo = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Docente)
                .Include(t => t.TipoEvaluacion)
                .Include(t => t.Archivos)
                .Include(t => t.Links)
                .Include(t => t.Entregas)
                .FirstOrDefaultAsync(t => t.Id == id && t.Activo);

            if (trabajo == null)
                return null;

            // Verificar que el estudiante está matriculado en el curso (más flexible - no requiere período activo)
            var estaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.IdEstudiante == idEstudiante 
                    && m.IdCurso == trabajo.IdCurso 
                    && m.Estado != "Retirado");

            if (!estaMatriculado)
                return null;

            var dto = MapToDto(trabajo);
            var entregaEstudiante = trabajo.Entregas.FirstOrDefault(e => e.IdEstudiante == idEstudiante);
            dto.YaEntregado = entregaEstudiante != null;
            dto.PuedeEntregar = DateTime.UtcNow <= trabajo.FechaLimite && !dto.YaEntregado;
            
            // Si ya entregó, incluir información de la entrega
            if (entregaEstudiante != null)
            {
                dto.Calificacion = entregaEstudiante.Calificacion;
                dto.ObservacionesDocente = entregaEstudiante.Observaciones;
                dto.FechaCalificacion = entregaEstudiante.FechaCalificacion;
                dto.FechaEntrega = entregaEstudiante.FechaEntrega;
            }

            return dto;
        }

        public async Task<List<TrabajoSimpleDto>> GetTrabajosPorCursoEstudianteAsync(int idCurso, int idEstudiante)
        {
            // Verificar matrícula (más flexible - no requiere período activo específico)
            // Verificar si el estudiante está matriculado en el curso (cualquier período, cualquier estado excepto "Retirado")
            var estaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.IdEstudiante == idEstudiante 
                    && m.IdCurso == idCurso 
                    && m.Estado != "Retirado");

            // Si no está matriculado, retornar lista vacía
            if (!estaMatriculado)
                return new List<TrabajoSimpleDto>();

            // Obtener trabajos activos del curso
            var trabajos = await _context.Set<TrabajoEncargado>()
                .Include(t => t.Curso)
                .Include(t => t.Entregas)
                .Where(t => t.IdCurso == idCurso && t.Activo)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            // Obtener IDs de entregas del estudiante para este curso
            var entregasEstudiante = await _context.Set<TrabajoEntrega>()
                .Where(e => e.IdEstudiante == idEstudiante 
                    && trabajos.Select(t => t.Id).Contains(e.IdTrabajo))
                .Select(e => e.IdTrabajo)
                .ToListAsync();

            return trabajos.Select(t => new TrabajoSimpleDto
            {
                Id = t.Id,
                IdCurso = t.IdCurso,
                NombreCurso = t.Curso?.NombreCurso,
                Titulo = t.Titulo,
                FechaCreacion = t.FechaCreacion,
                FechaLimite = t.FechaLimite,
                Activo = t.Activo,
                TotalEntregas = t.Entregas.Count,
                YaEntregado = entregasEstudiante.Contains(t.Id)
            }).ToList();
        }

        // ============================================
        // OPERACIONES DE ENTREGA
        // ============================================

        public async Task<(bool success, string? error, EntregaDto? created)> CrearEntregaAsync(
            EntregaCreateDto dto, int idEstudiante)
        {
            try
            {
                var trabajo = await _context.Set<TrabajoEncargado>()
                    .FirstOrDefaultAsync(t => t.Id == dto.IdTrabajo && t.Activo);

                if (trabajo == null)
                    return (false, "El trabajo no existe o no está activo", null);

                // Verificar que no haya entregado antes
                var entregaExistente = await _context.Set<TrabajoEntrega>()
                    .FirstOrDefaultAsync(e => e.IdTrabajo == dto.IdTrabajo && e.IdEstudiante == idEstudiante);

                if (entregaExistente != null)
                    return (false, "Ya has entregado este trabajo", null);

                // Verificar fecha límite
                var entregadoTarde = DateTime.UtcNow > trabajo.FechaLimite;

                var entrega = new TrabajoEntrega
                {
                    IdTrabajo = dto.IdTrabajo,
                    IdEstudiante = idEstudiante,
                    Comentario = dto.Comentario,
                    FechaEntrega = DateTime.UtcNow,
                    EntregadoTarde = entregadoTarde
                };

                _context.Set<TrabajoEntrega>().Add(entrega);
                await _context.SaveChangesAsync();

                // Procesar links (archivos se procesan en el controlador)
                if (dto.Links != null && dto.Links.Any())
                {
                    foreach (var linkDto in dto.Links)
                    {
                        var link = new TrabajoEntregaLink
                        {
                            IdEntrega = entrega.Id,
                            Url = linkDto.Url,
                            Descripcion = linkDto.Descripcion,
                            FechaCreacion = DateTime.UtcNow
                        };
                        _context.Set<TrabajoEntregaLink>().Add(link);
                    }
                    await _context.SaveChangesAsync();
                }

                var entregaDto = await GetEntregaAsync(entrega.Id);
                return (true, null, entregaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear entrega");
                return (false, $"Error al crear entrega: {ex.Message}", null);
            }
        }

        public async Task<(bool notFound, bool success, string? error)> ActualizarEntregaAsync(
            int idEntrega, EntregaUpdateDto dto, int idEstudiante)
        {
            try
            {
                var entrega = await _context.Set<TrabajoEntrega>()
                    .Include(e => e.Archivos)
                    .Include(e => e.Links)
                    .FirstOrDefaultAsync(e => e.Id == idEntrega && e.IdEstudiante == idEstudiante);

                if (entrega == null)
                    return (true, false, null);

                // Verificar que no esté calificado
                if (entrega.Calificacion.HasValue)
                    return (false, false, "No se puede modificar una entrega ya calificada");

                if (dto.Comentario != null)
                    entrega.Comentario = dto.Comentario;

                // Eliminar archivos
                if (dto.ArchivosEliminar != null && dto.ArchivosEliminar.Any())
                {
                    var archivosEliminar = await _context.Set<TrabajoEntregaArchivo>()
                        .Where(a => dto.ArchivosEliminar.Contains(a.Id) && a.IdEntrega == idEntrega)
                        .ToListAsync();

                    foreach (var archivo in archivosEliminar)
                    {
                        if (File.Exists(archivo.RutaArchivo))
                        {
                            try { File.Delete(archivo.RutaArchivo); } catch { }
                        }
                        _context.Set<TrabajoEntregaArchivo>().Remove(archivo);
                    }
                }

                // Eliminar links
                if (dto.LinksEliminar != null && dto.LinksEliminar.Any())
                {
                    var linksEliminar = await _context.Set<TrabajoEntregaLink>()
                        .Where(l => dto.LinksEliminar.Contains(l.Id) && l.IdEntrega == idEntrega)
                        .ToListAsync();

                    _context.Set<TrabajoEntregaLink>().RemoveRange(linksEliminar);
                }

                // Agregar nuevos links
                if (dto.LinksNuevos != null && dto.LinksNuevos.Any())
                {
                    foreach (var linkDto in dto.LinksNuevos)
                    {
                        var link = new TrabajoEntregaLink
                        {
                            IdEntrega = entrega.Id,
                            Url = linkDto.Url,
                            Descripcion = linkDto.Descripcion,
                            FechaCreacion = DateTime.UtcNow
                        };
                        _context.Set<TrabajoEntregaLink>().Add(link);
                    }
                }

                await _context.SaveChangesAsync();
                return (false, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar entrega");
                return (false, false, $"Error al actualizar entrega: {ex.Message}");
            }
        }

        public async Task<List<EntregaDto>> GetEntregasPorTrabajoAsync(int idTrabajo, int idDocente)
        {
            // Si idDocente es 0, es un administrador y puede ver todas las entregas
            if (idDocente == 0)
            {
                var trabajo = await _context.Set<TrabajoEncargado>()
                    .FirstOrDefaultAsync(t => t.Id == idTrabajo);

                if (trabajo == null)
                    return new List<EntregaDto>();
            }
            else
            {
                // Verificar que el trabajo pertenece al docente
                var trabajo = await _context.Set<TrabajoEncargado>()
                    .FirstOrDefaultAsync(t => t.Id == idTrabajo && t.IdDocente == idDocente);

                if (trabajo == null)
                    return new List<EntregaDto>();
            }

            var entregas = await _context.Set<TrabajoEntrega>()
                .Include(e => e.Estudiante)
                .Include(e => e.Trabajo)
                .Include(e => e.Archivos)
                .Include(e => e.Links)
                .Where(e => e.IdTrabajo == idTrabajo)
                .OrderByDescending(e => e.FechaEntrega)
                .ToListAsync();

            return entregas.Select(MapToEntregaDto).ToList();
        }

        public async Task<EntregaDto?> GetEntregaAsync(int idEntrega)
        {
            var entrega = await _context.Set<TrabajoEntrega>()
                .Include(e => e.Estudiante)
                .Include(e => e.Trabajo)
                .Include(e => e.Archivos)
                .Include(e => e.Links)
                .FirstOrDefaultAsync(e => e.Id == idEntrega);

            return entrega != null ? MapToEntregaDto(entrega) : null;
        }

        public async Task<(bool notFound, bool success, string? error)> CalificarEntregaAsync(
            int idEntrega, CalificarEntregaDto dto, int idDocente)
        {
            try
            {
                var entrega = await _context.Set<TrabajoEntrega>()
                    .Include(e => e.Trabajo)
                        .ThenInclude(t => t!.TipoEvaluacion)
                    .Include(e => e.Estudiante)
                    .FirstOrDefaultAsync(e => e.Id == idEntrega);

                if (entrega == null)
                    return (true, false, null);

                // Verificar que el trabajo pertenece al docente (o es administrador con idDocente = 0)
                if (idDocente > 0 && entrega.Trabajo?.IdDocente != idDocente)
                    return (false, false, "No tienes permiso para calificar esta entrega");

                entrega.Calificacion = dto.Calificacion;
                entrega.Observaciones = dto.Observaciones;
                entrega.FechaCalificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Registrar la nota en la tabla Nota si el trabajo tiene un tipo de evaluación asignado
                if (entrega.Trabajo?.IdTipoEvaluacion.HasValue == true)
                {
                    // Asegurarse de que el TipoEvaluacion esté cargado
                    if (entrega.Trabajo.TipoEvaluacion == null)
                    {
                        await _context.Entry(entrega.Trabajo)
                            .Reference(t => t.TipoEvaluacion)
                            .LoadAsync();
                    }

                    if (entrega.Trabajo.TipoEvaluacion != null)
                    {
                        _logger.LogInformation($"Registrando nota para entrega {entrega.Id} con tipo de evaluación: {entrega.Trabajo.TipoEvaluacion.Nombre}");
                        await RegistrarNotaAsync(entrega, entrega.Trabajo.TipoEvaluacion);
                    }
                    else
                    {
                        _logger.LogWarning($"El trabajo {entrega.Trabajo.Id} tiene IdTipoEvaluacion {entrega.Trabajo.IdTipoEvaluacion} pero el TipoEvaluacion no se pudo cargar");
                    }
                }
                else
                {
                    _logger.LogInformation($"El trabajo {entrega.Trabajo?.Id} no tiene tipo de evaluación asignado (IdTipoEvaluacion: {entrega.Trabajo?.IdTipoEvaluacion})");
                }

                return (false, true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calificar entrega");
                return (false, false, $"Error al calificar entrega: {ex.Message}");
            }
        }

        private async Task RegistrarNotaAsync(TrabajoEntrega entrega, TipoEvaluacion tipoEvaluacion)
        {
            try
            {
                _logger.LogInformation($"RegistrarNotaAsync iniciado - EntregaId: {entrega.Id}, EstudianteId: {entrega.IdEstudiante}, CursoId: {entrega.Trabajo?.IdCurso}, TipoEvaluacion: {tipoEvaluacion.Nombre}");

                if (entrega.Trabajo == null)
                {
                    _logger.LogWarning("El trabajo asociado a la entrega es null");
                    return;
                }

                if (!entrega.Calificacion.HasValue)
                {
                    _logger.LogWarning("La entrega no tiene calificación asignada");
                    return;
                }

                // Obtener la matrícula del estudiante en el curso
                // Buscar primero en el período activo, si no existe, buscar en cualquier período
                var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
                
                var matricula = periodoActivo != null
                    ? await _context.Matriculas
                        .FirstOrDefaultAsync(m => m.IdEstudiante == entrega.IdEstudiante 
                            && m.IdCurso == entrega.Trabajo.IdCurso
                            && m.IdPeriodo == periodoActivo.Id
                            && (m.Estado == "Matriculado" || m.Estado == "Activo"))
                    : await _context.Matriculas
                        .FirstOrDefaultAsync(m => m.IdEstudiante == entrega.IdEstudiante 
                            && m.IdCurso == entrega.Trabajo.IdCurso
                            && m.Estado != "Retirado");

                if (matricula == null)
                {
                    _logger.LogWarning($"No se encontró matrícula para estudiante {entrega.IdEstudiante} en curso {entrega.Trabajo.IdCurso}. Buscando todas las matrículas del estudiante...");
                    
                    // Log para debugging: ver todas las matrículas del estudiante
                    var todasMatriculas = await _context.Matriculas
                        .Where(m => m.IdEstudiante == entrega.IdEstudiante)
                        .ToListAsync();
                    _logger.LogInformation($"Total de matrículas del estudiante {entrega.IdEstudiante}: {todasMatriculas.Count}");
                    foreach (var m in todasMatriculas)
                    {
                        _logger.LogInformation($"  - Matrícula ID: {m.Id}, Curso: {m.IdCurso}, Estado: {m.Estado}, Período: {m.IdPeriodo}");
                    }
                    
                    // Intentar buscar sin restricción de período
                    matricula = await _context.Matriculas
                        .Where(m => m.IdEstudiante == entrega.IdEstudiante 
                            && m.IdCurso == entrega.Trabajo.IdCurso
                            && m.Estado != "Retirado")
                        .OrderByDescending(m => m.IdPeriodo)
                        .FirstOrDefaultAsync();
                    
                    if (matricula == null)
                    {
                        _logger.LogError($"No se encontró matrícula válida para estudiante {entrega.IdEstudiante} en curso {entrega.Trabajo.IdCurso}");
                        return;
                    }
                    else
                    {
                        _logger.LogInformation($"Matrícula encontrada sin restricción de período: ID {matricula.Id}, Estado: {matricula.Estado}, Período: {matricula.IdPeriodo}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Matrícula encontrada: ID {matricula.Id}, Estado: {matricula.Estado}, Período: {matricula.IdPeriodo}");
                }

                // Verificar si el trabajo está dividido
                var trabajo = entrega.Trabajo;
                bool esTrabajoDividido = trabajo != null && trabajo.TotalTrabajos.HasValue && trabajo.TotalTrabajos.Value > 1;

                if (esTrabajoDividido)
                {
                    // Si el trabajo está dividido, verificar si todos los trabajos están calificados
                    var todosTrabajos = await _context.Set<TrabajoEncargado>()
                        .Where(t => t.IdCurso == trabajo.IdCurso
                            && t.IdTipoEvaluacion == trabajo.IdTipoEvaluacion
                            && t.NumeroTrabajo.HasValue
                            && t.Activo)
                        .OrderBy(t => t.NumeroTrabajo)
                        .ToListAsync();

                    var entregasTrabajos = new List<(int NumeroTrabajo, decimal? Calificacion)>();
                    
                    foreach (var t in todosTrabajos)
                    {
                        var entregaEstudiante = await _context.Set<TrabajoEntrega>()
                            .FirstOrDefaultAsync(e => e.IdTrabajo == t.Id && e.IdEstudiante == entrega.IdEstudiante);
                        
                        entregasTrabajos.Add((t.NumeroTrabajo!.Value, entregaEstudiante?.Calificacion));
                    }

                    // Verificar si todos los trabajos están calificados
                    bool todosCalificados = entregasTrabajos.All(e => e.Calificacion.HasValue);

                    if (!todosCalificados)
                    {
                        _logger.LogInformation($"Trabajo dividido - Faltan trabajos por calificar. Total: {todosTrabajos.Count}, Calificados: {entregasTrabajos.Count(e => e.Calificacion.HasValue)}");
                        return; // No registrar nota hasta que todos estén calificados
                    }

                    // Calcular promedio ponderado de todos los trabajos
                    decimal promedioPonderado = 0;
                    decimal pesoTotalUsado = 0;
                    
                    foreach (var (numero, calificacion) in entregasTrabajos)
                    {
                        var trabajoCorrespondiente = todosTrabajos.First(t => t.NumeroTrabajo == numero);
                        var pesoIndividual = trabajoCorrespondiente.PesoIndividual ?? tipoEvaluacion.Peso / todosTrabajos.Count;
                        
                        promedioPonderado += calificacion!.Value * pesoIndividual;
                        pesoTotalUsado += pesoIndividual;
                    }

                    if (pesoTotalUsado > 0)
                    {
                        promedioPonderado = promedioPonderado / pesoTotalUsado * tipoEvaluacion.Peso / 100m;
                    }

                    _logger.LogInformation($"Trabajo dividido - Todos calificados. Promedio calculado: {promedioPonderado:F2}");

                    // Registrar o actualizar la nota con el promedio
                    var tipoEvaluacionNombre = tipoEvaluacion.Nombre;
                    var notaExistente = await _context.Notas
                        .FirstOrDefaultAsync(n => n.IdMatricula == matricula.Id 
                            && n.TipoEvaluacion == tipoEvaluacionNombre);

                    if (notaExistente != null)
                    {
                        notaExistente.NotaValor = promedioPonderado;
                        notaExistente.Peso = tipoEvaluacion.Peso;
                        notaExistente.Fecha = entrega.FechaCalificacion ?? DateTime.UtcNow;
                        notaExistente.Observaciones = entrega.Observaciones;
                        notaExistente.FechaRegistro = DateTime.UtcNow;
                        _logger.LogInformation($"Nota actualizada (promedio dividido) - MatrículaId: {matricula.Id}, TipoEvaluacion: '{tipoEvaluacion.Nombre}', Nota: {promedioPonderado:F2}, Peso: {tipoEvaluacion.Peso}");
                    }
                    else
                    {
                        var nuevaNota = new Nota
                        {
                            IdMatricula = matricula.Id,
                            TipoEvaluacion = tipoEvaluacion.Nombre.Trim(),
                            NotaValor = promedioPonderado,
                            Peso = tipoEvaluacion.Peso,
                            Fecha = entrega.FechaCalificacion ?? DateTime.UtcNow,
                            FechaRegistro = DateTime.UtcNow,
                            Observaciones = entrega.Observaciones
                        };
                        _context.Notas.Add(nuevaNota);
                        _logger.LogInformation($"Nota creada (promedio dividido) - MatrículaId: {matricula.Id}, TipoEvaluacion: {tipoEvaluacion.Nombre}, Nota: {promedioPonderado:F2}, Peso: {tipoEvaluacion.Peso}");
                    }
                }
                else
                {
                    // Trabajo no dividido - comportamiento normal
                    var tipoEvaluacionNombre = tipoEvaluacion.Nombre;
                    var notaExistente = await _context.Notas
                        .FirstOrDefaultAsync(n => n.IdMatricula == matricula.Id 
                            && n.TipoEvaluacion == tipoEvaluacionNombre);
                    
                    _logger.LogInformation($"Buscando nota existente - MatrículaId: {matricula.Id}, TipoEvaluacion: '{tipoEvaluacionNombre}'. Encontrada: {notaExistente != null}");

                    if (notaExistente != null)
                    {
                        // Actualizar la nota existente
                        notaExistente.NotaValor = entrega.Calificacion.Value;
                        notaExistente.Peso = tipoEvaluacion.Peso;
                        notaExistente.Fecha = entrega.FechaCalificacion ?? DateTime.UtcNow;
                        notaExistente.Observaciones = entrega.Observaciones;
                        notaExistente.FechaRegistro = DateTime.UtcNow;
                        _logger.LogInformation($"Nota actualizada - MatrículaId: {matricula.Id}, TipoEvaluacion: '{tipoEvaluacion.Nombre}', Nota: {entrega.Calificacion.Value}, Peso: {tipoEvaluacion.Peso}");
                    }
                    else
                    {
                        // Crear nueva nota
                        var fechaCalificacion = entrega.FechaCalificacion ?? DateTime.UtcNow;
                        var nuevaNota = new Nota
                        {
                            IdMatricula = matricula.Id,
                            TipoEvaluacion = tipoEvaluacion.Nombre.Trim(),
                            NotaValor = entrega.Calificacion.Value,
                            Peso = tipoEvaluacion.Peso,
                            Fecha = fechaCalificacion,
                            FechaRegistro = DateTime.UtcNow,
                            Observaciones = entrega.Observaciones
                        };
                        _context.Notas.Add(nuevaNota);
                        _logger.LogInformation($"Nota creada - MatrículaId: {matricula.Id}, TipoEvaluacion: {tipoEvaluacion.Nombre}, Nota: {entrega.Calificacion.Value}, Peso: {tipoEvaluacion.Peso}");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Nota guardada exitosamente en la base de datos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al registrar nota para entrega {entrega.Id}. Detalles: {ex.Message}");
                // No lanzamos excepción para no afectar la calificación del trabajo
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS
        // ============================================

        private TrabajoDto MapToDto(TrabajoEncargado trabajo)
        {
            return new TrabajoDto
            {
                Id = trabajo.Id,
                IdCurso = trabajo.IdCurso,
                NombreCurso = trabajo.Curso?.NombreCurso,
                IdDocente = trabajo.IdDocente,
                NombreDocente = $"{trabajo.Docente?.Nombres} {trabajo.Docente?.Apellidos}",
                Titulo = trabajo.Titulo,
                Descripcion = trabajo.Descripcion,
                FechaCreacion = trabajo.FechaCreacion,
                FechaLimite = trabajo.FechaLimite,
                Activo = trabajo.Activo,
                FechaActualizacion = trabajo.FechaActualizacion,
                Archivos = trabajo.Archivos.Select(a => new ArchivoDto
                {
                    Id = a.Id,
                    NombreArchivo = a.NombreArchivo,
                    RutaArchivo = a.RutaArchivo,
                    TipoArchivo = a.TipoArchivo,
                    Tamaño = a.Tamaño,
                    FechaSubida = a.FechaSubida
                }).ToList(),
                Links = trabajo.Links.Select(l => new LinkDto
                {
                    Id = l.Id,
                    Url = l.Url,
                    Descripcion = l.Descripcion,
                    FechaCreacion = l.FechaCreacion
                }).ToList(),
                TotalEntregas = trabajo.Entregas.Count,
                PuedeEntregar = DateTime.UtcNow <= trabajo.FechaLimite,
                YaEntregado = false,
                IdTipoEvaluacion = trabajo.IdTipoEvaluacion,
                NombreTipoEvaluacion = trabajo.TipoEvaluacion?.Nombre,
                PesoTipoEvaluacion = trabajo.TipoEvaluacion?.Peso,
                NumeroTrabajo = trabajo.NumeroTrabajo,
                TotalTrabajos = trabajo.TotalTrabajos,
                PesoIndividual = trabajo.PesoIndividual
            };
        }

        /// <summary>
        /// Recalcula los pesos individuales de todos los trabajos de un tipo de evaluación dividido
        /// </summary>
        private async Task RecalcularPesosTrabajosAsync(int idCurso, int idTipoEvaluacion, int totalTrabajos)
        {
            try
            {
                var tipoEvaluacion = await _context.TiposEvaluacion
                    .FirstOrDefaultAsync(t => t.Id == idTipoEvaluacion && t.IdCurso == idCurso);

                if (tipoEvaluacion == null)
                {
                    _logger.LogWarning($"No se encontró el tipo de evaluación {idTipoEvaluacion} para recalcular pesos");
                    return;
                }

                var pesoIndividual = tipoEvaluacion.Peso / totalTrabajos;

                var trabajos = await _context.Set<TrabajoEncargado>()
                    .Where(t => t.IdCurso == idCurso
                        && t.IdTipoEvaluacion == idTipoEvaluacion
                        && t.NumeroTrabajo.HasValue
                        && t.Activo)
                    .ToListAsync();

                foreach (var trabajo in trabajos)
                {
                    trabajo.PesoIndividual = pesoIndividual;
                    trabajo.TotalTrabajos = totalTrabajos;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Pesos recalculados para {trabajos.Count} trabajos del tipo de evaluación {tipoEvaluacion.Nombre}. Peso individual: {pesoIndividual}%");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al recalcular pesos de trabajos para tipo de evaluación {idTipoEvaluacion}");
            }
        }

        private EntregaDto MapToEntregaDto(TrabajoEntrega entrega)
        {
            return new EntregaDto
            {
                Id = entrega.Id,
                IdTrabajo = entrega.IdTrabajo,
                TituloTrabajo = entrega.Trabajo?.Titulo,
                IdEstudiante = entrega.IdEstudiante,
                NombreEstudiante = $"{entrega.Estudiante?.Nombres} {entrega.Estudiante?.Apellidos}",
                CodigoEstudiante = entrega.Estudiante?.Codigo,
                Comentario = entrega.Comentario,
                FechaEntrega = entrega.FechaEntrega,
                Calificacion = entrega.Calificacion,
                Observaciones = entrega.Observaciones,
                FechaCalificacion = entrega.FechaCalificacion,
                EntregadoTarde = entrega.EntregadoTarde,
                Archivos = entrega.Archivos.Select(a => new ArchivoDto
                {
                    Id = a.Id,
                    NombreArchivo = a.NombreArchivo,
                    RutaArchivo = a.RutaArchivo,
                    TipoArchivo = a.TipoArchivo,
                    Tamaño = a.Tamaño,
                    FechaSubida = a.FechaSubida
                }).ToList(),
                Links = entrega.Links.Select(l => new LinkDto
                {
                    Id = l.Id,
                    Url = l.Url,
                    Descripcion = l.Descripcion,
                    FechaCreacion = l.FechaCreacion
                }).ToList()
            };
        }

        private async Task NotificarEstudiantesAsync(int idTrabajo, int idCurso, string tituloTrabajo)
        {
            try
            {
                var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
                if (periodoActivo == null) return;

                var estudiantes = await _context.Matriculas
                    .Include(m => m.Estudiante)
                    .Where(m => m.IdCurso == idCurso 
                        && m.IdPeriodo == periodoActivo.Id 
                        && m.Estado == "Activo"
                        && m.Estudiante != null
                        && m.Estudiante.IdUsuario > 0)
                    .Select(m => new { m.Estudiante!.IdUsuario, m.Estudiante.Nombres, m.Estudiante.Apellidos })
                    .ToListAsync();

                var curso = await _context.Cursos.FindAsync(idCurso);

                foreach (var estudiante in estudiantes)
                {
                    if (estudiante.IdUsuario <= 0) continue;

                    var notificacion = new Notificacion
                    {
                        Tipo = "academico",
                        Accion = "trabajo_encargado",
                        Mensaje = $"Nuevo trabajo encargado: {tituloTrabajo} - {curso?.NombreCurso}",
                        MetadataJson = JsonSerializer.Serialize(new
                        {
                            idTrabajo = idTrabajo,
                            idCurso = idCurso,
                            titulo = tituloTrabajo,
                            nombreCurso = curso?.NombreCurso
                        }),
                        IdUsuario = estudiante.IdUsuario,
                        FechaCreacion = DateTime.UtcNow,
                        Leida = false
                    };

                    _context.Notificaciones.Add(notificacion);
                }

                await _context.SaveChangesAsync();

                // Enviar notificaciones vía SignalR
                foreach (var estudiante in estudiantes)
                {
                    if (estudiante.IdUsuario <= 0) continue;

                    try
                    {
                        var hubType = Type.GetType("API_REST_CURSOSACADEMICOS.Hubs.NotificationsHub, API_REST_CURSOSACADEMICOS");
                        if (hubType != null)
                        {
                            var hubContextType = typeof(IHubContext<>).MakeGenericType(hubType);
                            var hubContext = _serviceProvider.GetService(hubContextType);

                            if (hubContext != null)
                            {
                                var notificacion = await _context.Notificaciones
                                    .Where(n => n.IdUsuario == estudiante.IdUsuario 
                                        && n.Accion == "trabajo_encargado" 
                                        && n.MetadataJson != null 
                                        && n.MetadataJson.Contains($"\"idTrabajo\":{idTrabajo}"))
                                    .OrderByDescending(n => n.FechaCreacion)
                                    .FirstOrDefaultAsync();

                                if (notificacion != null)
                                {
                                    var notificationDto = new
                                    {
                                        id = notificacion.Id,
                                        tipo = notificacion.Tipo,
                                        accion = notificacion.Accion,
                                        mensaje = notificacion.Mensaje,
                                        metadata = JsonSerializer.Deserialize<object>(notificacion.MetadataJson ?? "{}"),
                                        fechaCreacion = notificacion.FechaCreacion,
                                        leida = notificacion.Leida
                                    };

                                    var clientsProperty = hubContextType.GetProperty("Clients");
                                    if (clientsProperty != null)
                                    {
                                        var clients = clientsProperty.GetValue(hubContext);
                                        var userMethod = clients?.GetType().GetMethod("User", new[] { typeof(string) });
                                        if (userMethod != null)
                                        {
                                            var userClient = userMethod.Invoke(clients, new object[] { estudiante.IdUsuario.ToString() });
                                            var sendAsyncMethod = userClient?.GetType().GetMethod("SendAsync", new[] { typeof(string), typeof(object) });
                                            if (sendAsyncMethod != null)
                                            {
                                                await (Task)sendAsyncMethod.Invoke(userClient, new object[] { "ReceiveNotification", notificationDto })!;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo enviar notificación SignalR para estudiante {IdUsuario}", estudiante.IdUsuario);
                    }
                }

                _logger.LogInformation("✅ Notificaciones enviadas para trabajo {IdTrabajo} a {Cantidad} estudiantes", 
                    idTrabajo, estudiantes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al notificar estudiantes sobre nuevo trabajo");
            }
        }

    }
}

