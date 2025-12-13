using API_REST_CURSOSACADEMICOS.Models;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface IUserLookupService
{
    Task<int?> GetDocenteIdByEmailAsync(string email);
    Task<Docente?> GetDocenteByEmailAsync(string email);
    Task<int?> GetEstudianteIdByUsuarioIdAsync(int usuarioId);
    Task<int?> GetEstudianteIdByEmailAsync(string email);
}


