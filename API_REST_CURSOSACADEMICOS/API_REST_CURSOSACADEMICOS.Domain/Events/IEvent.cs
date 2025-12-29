namespace API_REST_CURSOSACADEMICOS.Domain.Events;

/// <summary>
/// Interfaz base para todos los eventos del dominio
/// </summary>
public interface IEvent
{
    Guid Id { get; }
    DateTime Timestamp { get; }
}

