using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface IDocentesService
{
    // Admin CRUD
    Task<List<DocenteDto>> GetDocentesAsync();
    Task<DocenteDto?> GetDocenteAsync(int id);
    Task<(bool success, string? error, DocenteDto? created)> CreateDocenteAsync(DocenteCreateDto docenteDto);
    Task<(bool notFound, bool success, string? error)> UpdateDocenteAsync(int id, DocenteUpdateDto docenteDto);
    Task<(bool notFound, bool hasAssignedCourses, bool success, string? error)> DeleteDocenteAsync(int id);

    // Docente autenticado
    Task<List<CursoDocenteDto>> GetMisCursosAsync(int docenteId);
    Task<ServiceOutcome> GetEstudiantesCursoAsync(int docenteId, int idCurso);

    Task<ServiceOutcome> RegistrarNotasAsync(int docenteId, int idCurso, System.Text.Json.JsonElement notasJson);
    Task<ServiceOutcome> ObtenerTiposEvaluacionAsync(int docenteId, int idCurso);
    Task<ServiceOutcome> ConfigurarTiposEvaluacionAsync(int docenteId, int idCurso, ConfigurarTiposEvaluacionDto configDto);

    Task<ServiceOutcome> RegistrarAsistenciaAsync(int docenteId, RegistrarAsistenciasMasivasDto asistenciaDto);
    Task<ServiceOutcome> GetAsistenciaCursoAsync(int docenteId, int idCurso, DateTime? fecha, string? tipoClase);
    Task<ServiceOutcome> GetResumenAsistenciaAsync(int docenteId, int idCurso);
}

public enum ServiceOutcomeStatus
{
    Ok,
    BadRequest,
    NotFound,
    Unauthorized,
    Forbidden
}

/// <summary>
/// Result pattern: evita acoplar Application a ASP.NET (IActionResult) y permite que el Controller traduzca.
/// </summary>
public record ServiceOutcome(ServiceOutcomeStatus Status, object? Payload = null)
{
    public static ServiceOutcome Ok(object? payload = null) => new(ServiceOutcomeStatus.Ok, payload);
    public static ServiceOutcome BadRequest(object? payload = null) => new(ServiceOutcomeStatus.BadRequest, payload);
    public static ServiceOutcome NotFound(object? payload = null) => new(ServiceOutcomeStatus.NotFound, payload);
    public static ServiceOutcome Unauthorized(object? payload = null) => new(ServiceOutcomeStatus.Unauthorized, payload);
    public static ServiceOutcome Forbidden(object? payload = null) => new(ServiceOutcomeStatus.Forbidden, payload);
}


