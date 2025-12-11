using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Hubs;
using API_REST_CURSOSACADEMICOS.Extensions;
using API_REST_CURSOSACADEMICOS.Models;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacionesController : ControllerBase
    {
        private readonly GestionAcademicaContext _context;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public NotificacionesController(GestionAcademicaContext context, IHubContext<NotificationsHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/notificaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificacionDto>>> GetNotificaciones([FromQuery] int? limit = 100)
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => n.IdUsuario == userId || n.IdUsuario == null) // notificaciones del usuario o globales
                .OrderByDescending(n => n.FechaCreacion)
                .Take(limit ?? 100)
                .ToListAsync();

            var result = notificaciones.Select(n => new NotificacionDto
            {
                Id = n.Id,
                Tipo = n.Tipo,
                Accion = n.Accion,
                Mensaje = n.Mensaje,
                Metadata = string.IsNullOrEmpty(n.MetadataJson) ? null : JsonSerializer.Deserialize<object>(n.MetadataJson),
                FechaCreacion = n.FechaCreacion,
                Leida = n.Leida
            }).ToList();

            return Ok(result);
        }

        // GET: api/notificaciones/no-leidas
        [HttpGet("no-leidas")]
        public async Task<ActionResult<int>> GetCountNoLeidas()
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var count = await _context.Notificaciones
                .Where(n => (n.IdUsuario == userId || n.IdUsuario == null) && !n.Leida)
                .CountAsync();

            return Ok(new { count });
        }

        // POST: api/notificaciones
        [HttpPost]
        public async Task<ActionResult<NotificacionDto>> CrearNotificacion([FromBody] NotificacionCreateDto dto)
        {
            var notificacion = new Notificacion
            {
                Tipo = dto.Tipo,
                Accion = dto.Accion,
                Mensaje = dto.Mensaje,
                MetadataJson = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null,
                IdUsuario = dto.IdUsuario,
                FechaCreacion = DateTime.Now,
                Leida = false
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            var result = new NotificacionDto
            {
                Id = notificacion.Id,
                Tipo = notificacion.Tipo,
                Accion = notificacion.Accion,
                Mensaje = notificacion.Mensaje,
                Metadata = dto.Metadata,
                FechaCreacion = notificacion.FechaCreacion,
                Leida = notificacion.Leida
            };

            // Enviar notificación en tiempo real a todos los clientes conectados
            // Si tiene IdUsuario específico, enviar solo a ese usuario, sino a todos
            if (dto.IdUsuario.HasValue)
            {
                await _hubContext.Clients.User(dto.IdUsuario.Value.ToString()).SendAsync("ReceiveNotification", result);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", result);
            }

            Console.WriteLine($"✅ Notificación creada y enviada: {notificacion.Tipo} - {notificacion.Accion}");

            return CreatedAtAction(nameof(GetNotificaciones), new { id = notificacion.Id }, result);
        }

        // PUT: api/notificaciones/marcar-leidas
        [HttpPut("marcar-leidas")]
        public async Task<IActionResult> MarcarComoLeidas([FromBody] NotificacionMarkReadDto dto)
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => dto.NotificacionIds.Contains(n.Id) && (n.IdUsuario == userId || n.IdUsuario == null))
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones marcadas como leídas", count = notificaciones.Count });
        }

        // DELETE: api/notificaciones
        [HttpDelete]
        public async Task<IActionResult> LimpiarNotificaciones()
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var notificaciones = await _context.Notificaciones
                .Where(n => n.IdUsuario == userId || n.IdUsuario == null)
                .ToListAsync();

            _context.Notificaciones.RemoveRange(notificaciones);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificaciones eliminadas", count = notificaciones.Count });
        }
    }
}
