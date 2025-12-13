using System.Text.Json;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public class NotificacionesService : INotificacionesService
{
    private readonly GestionAcademicaContext _context;

    public NotificacionesService(GestionAcademicaContext context)
    {
        _context = context;
    }

    public async Task<List<NotificacionDto>> GetNotificacionesAsync(int userId, int limit)
    {
        var notificaciones = await _context.Notificaciones
            .Where(n => n.IdUsuario == userId || n.IdUsuario == null)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(limit)
            .ToListAsync();

        return notificaciones.Select(n => new NotificacionDto
        {
            Id = n.Id,
            Tipo = n.Tipo,
            Accion = n.Accion,
            Mensaje = n.Mensaje,
            Metadata = string.IsNullOrEmpty(n.MetadataJson) ? null : JsonSerializer.Deserialize<object>(n.MetadataJson),
            FechaCreacion = n.FechaCreacion,
            Leida = n.Leida
        }).ToList();
    }

    public async Task<int> GetCountNoLeidasAsync(int userId)
    {
        return await _context.Notificaciones
            .Where(n => (n.IdUsuario == userId || n.IdUsuario == null) && !n.Leida)
            .CountAsync();
    }

    public async Task<NotificacionDto> CrearNotificacionAsync(NotificacionCreateDto dto)
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

        return new NotificacionDto
        {
            Id = notificacion.Id,
            Tipo = notificacion.Tipo,
            Accion = notificacion.Accion,
            Mensaje = notificacion.Mensaje,
            Metadata = dto.Metadata,
            FechaCreacion = notificacion.FechaCreacion,
            Leida = notificacion.Leida
        };
    }

    public async Task<int> MarcarComoLeidasAsync(int userId, List<int> notificacionIds)
    {
        var notificaciones = await _context.Notificaciones
            .Where(n => notificacionIds.Contains(n.Id) && (n.IdUsuario == userId || n.IdUsuario == null))
            .ToListAsync();

        foreach (var notificacion in notificaciones)
        {
            notificacion.Leida = true;
        }

        await _context.SaveChangesAsync();
        return notificaciones.Count;
    }

    public async Task<int> LimpiarNotificacionesAsync(int userId)
    {
        var notificaciones = await _context.Notificaciones
            .Where(n => n.IdUsuario == userId || n.IdUsuario == null)
            .ToListAsync();

        _context.Notificaciones.RemoveRange(notificaciones);
        await _context.SaveChangesAsync();
        return notificaciones.Count;
    }
}


