using API_REST_CURSOSACADEMICOS.Domain.Events;

namespace API_REST_CURSOSACADEMICOS.Application.Events;

/// <summary>
/// Interfaz para handlers de eventos
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event);
}

