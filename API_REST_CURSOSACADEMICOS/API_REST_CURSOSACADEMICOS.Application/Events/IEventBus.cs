using API_REST_CURSOSACADEMICOS.Domain.Events;

namespace API_REST_CURSOSACADEMICOS.Application.Events;

/// <summary>
/// Interfaz para el Event Bus que maneja la publicación y suscripción de eventos
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publica un evento en el bus
    /// </summary>
    Task PublishAsync<T>(T @event) where T : IEvent;

    /// <summary>
    /// Suscribe un handler a un tipo de evento
    /// </summary>
    void Subscribe<T, TH>() 
        where T : IEvent
        where TH : IEventHandler<T>;
}

