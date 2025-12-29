using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface ITrabajoService
{
    // Operaciones para docentes
    Task<List<TrabajoDto>> GetTrabajosPorCursoAsync(int idCurso);
    Task<List<TrabajoDto>> GetTrabajosPorDocenteAsync(int idDocente);
    Task<TrabajoDto?> GetTrabajoAsync(int id);
    Task<(bool success, string? error, TrabajoDto? created)> CreateTrabajoAsync(TrabajoCreateDto dto, int idDocente);
    Task<(bool notFound, bool success, string? error)> UpdateTrabajoAsync(int id, TrabajoUpdateDto dto, int idDocente);
    Task<(bool notFound, bool success, string? error)> DeleteTrabajoAsync(int id, int idDocente);

    // Operaciones para estudiantes
    Task<List<TrabajoSimpleDto>> GetTrabajosDisponiblesAsync(int idEstudiante);
    Task<TrabajoDto?> GetTrabajoParaEstudianteAsync(int id, int idEstudiante);
    Task<List<TrabajoSimpleDto>> GetTrabajosPorCursoEstudianteAsync(int idCurso, int idEstudiante);

    // Operaciones de entrega
    Task<(bool success, string? error, EntregaDto? created)> CrearEntregaAsync(EntregaCreateDto dto, int idEstudiante);
    Task<(bool notFound, bool success, string? error)> ActualizarEntregaAsync(int idEntrega, EntregaUpdateDto dto, int idEstudiante);
    Task<List<EntregaDto>> GetEntregasPorTrabajoAsync(int idTrabajo, int idDocente);
    Task<EntregaDto?> GetEntregaAsync(int idEntrega);
    Task<(bool notFound, bool success, string? error)> CalificarEntregaAsync(int idEntrega, CalificarEntregaDto dto, int idDocente);
}

