using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public interface ICursosService
{
    Task<List<CursoDto>> GetCursosAsync();
    Task<CursoDto?> GetCursoAsync(int id);
    Task<(bool exists, List<CursoDto> cursos)> GetCursosPorDocenteAsync(int docenteId);
    Task<List<CursoDto>> GetCursosPorCicloAsync(int ciclo);

    Task<(bool success, string? error, CursoDto? created)> CreateCursoAsync(CursoCreateDto dto);
    Task<(bool notFound, bool success, string? error)> UpdateCursoAsync(int id, CursoUpdateDto dto);
    Task<bool> DeleteCursoAsync(int id);
}


