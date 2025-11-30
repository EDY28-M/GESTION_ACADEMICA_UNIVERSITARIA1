using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces
{
    public interface IEstudianteService
    {
        Task<EstudianteDto?> GetByUsuarioIdAsync(int usuarioId);
        Task<List<CursoDisponibleDto>> GetCursosDisponiblesAsync(int cicloActual, int idPeriodo);
        Task<List<CursoDisponibleDto>> GetCursosDisponiblesPorEstudianteAsync(int idEstudiante);
        Task<List<MatriculaDto>> GetMisCursosAsync(int idEstudiante, int? idPeriodo = null);
        Task<MatriculaDto> MatricularAsync(int idEstudiante, MatricularDto dto, bool isAutorizado = false);
        Task RetirarAsync(int idMatricula, int idEstudiante);
        Task<List<NotaDto>> GetNotasAsync(int idEstudiante, int? idPeriodo = null);
        Task<PeriodoDto?> GetPeriodoActivoAsync();
        Task<List<PeriodoDto>> GetPeriodosAsync();
        Task<RegistroNotasDto> GetRegistroNotasAsync(int idEstudiante);
    }
}
