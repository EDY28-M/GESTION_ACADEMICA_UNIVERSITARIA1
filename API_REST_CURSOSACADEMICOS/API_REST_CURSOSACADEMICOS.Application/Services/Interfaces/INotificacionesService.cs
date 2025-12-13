using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface INotificacionesService
{
    Task<List<NotificacionDto>> GetNotificacionesAsync(int userId, int limit);
    Task<int> GetCountNoLeidasAsync(int userId);

    Task<NotificacionDto> CrearNotificacionAsync(NotificacionCreateDto dto);
    Task<int> MarcarComoLeidasAsync(int userId, List<int> notificacionIds);
    Task<int> LimpiarNotificacionesAsync(int userId);
}


