namespace API_REST_CURSOSACADEMICOS.Domain.Events;

/// <summary>
/// Evento que se dispara cuando un estudiante se matricula en un curso
/// </summary>
public class EstudianteMatriculadoEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public int IdEstudiante { get; set; }
    public int IdUsuario { get; set; }
    public int IdCurso { get; set; }
    public string NombreCurso { get; set; } = string.Empty;
    public int IdPeriodo { get; set; }
    public string NombrePeriodo { get; set; } = string.Empty;
    public int IdMatricula { get; set; }
}

