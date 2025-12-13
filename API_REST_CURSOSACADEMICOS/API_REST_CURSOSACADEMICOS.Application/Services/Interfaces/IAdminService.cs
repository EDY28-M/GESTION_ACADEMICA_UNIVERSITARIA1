using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface IAdminService
{
    Task<ServiceOutcome> GetTodosEstudiantesAsync();
    Task<ServiceOutcome> GetEstudianteDetalleAsync(int id);
    Task<ServiceOutcome> CrearEstudianteAsync(CrearEstudianteDto dto);
    Task<ServiceOutcome> EliminarEstudianteAsync(int id);
    Task<ServiceOutcome> CrearCursosDirigidosAsync(MatriculaDirigidaDto dto);

    Task<ServiceOutcome> GetPeriodosAsync();
    Task<ServiceOutcome> CrearPeriodoAsync(CrearPeriodoDto dto);
    Task<ServiceOutcome> EditarPeriodoAsync(int id, EditarPeriodoDto dto);
    Task<ServiceOutcome> ActivarPeriodoAsync(int id);
    Task<ServiceOutcome> EliminarPeriodoAsync(int id);

    Task<ServiceOutcome> GetTodosDocentesAsync();
    Task<ServiceOutcome> CrearDocenteAsync(CrearDocenteConPasswordDto dto);
    Task<ServiceOutcome> AsignarPasswordDocenteAsync(int id, AsignarPasswordDto dto);
    Task<ServiceOutcome> ActualizarDocenteAsync(int id, ActualizarDocenteDto dto);
    Task<ServiceOutcome> EliminarDocenteAsync(int id);

    Task<ServiceOutcome> CerrarPeriodoAsync(int id);
    Task<ServiceOutcome> AbrirPeriodoAsync(int id);
    Task<ServiceOutcome> ValidarCierrePeriodoAsync(int id);
}


