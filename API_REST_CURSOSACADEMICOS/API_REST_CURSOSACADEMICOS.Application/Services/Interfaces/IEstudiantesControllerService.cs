using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface IEstudiantesControllerService
{
    Task<List<EstudianteDto>> GetAllAdminAsync();
    Task<EstudianteDto?> GetByIdAdminAsync(int id);

    Task<List<int>> GetCursosMatriculadosIdsAsync(int idEstudiante, int idPeriodo);

    Task<object> VerificarPrerequisitosAsync(int idEstudiante, int idCurso);

    Task<List<OrdenMeritoDto>> GetOrdenMeritoAsync(string? promocion);
    Task<List<string>> GetPromocionesAsync();
    Task<ServiceOutcome> GetMiPosicionMeritoAsync(int usuarioId);

    Task<ServiceOutcome> CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto request);
    Task<ServiceOutcome> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto request);
}


