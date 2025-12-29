using System.Collections.Generic;
using System.Threading.Tasks;
using API_REST_CURSOSACADEMICOS.DTOs;

namespace API_REST_CURSOSACADEMICOS.Services.Interfaces
{
    public interface IHorarioService
    {
        Task<HorarioDto> CrearHorarioAsync(CrearHorarioDto horarioDto);
        Task<bool> EliminarHorarioAsync(int id);
        Task<IEnumerable<HorarioDto>> ObtenerPorCursoAsync(int idCurso);
        Task<IEnumerable<HorarioDto>> ObtenerPorDocenteAsync(int idDocente);
        Task<IEnumerable<HorarioDto>> ObtenerPorEstudianteAsync(int idEstudiante);
        Task<HorarioConflictoDto> ValidarCruceHorarioAsync(CrearHorarioDto horarioDto, int? idHorarioExcluir = null);
        
        // Nuevos métodos para gestión de horarios por docente
        Task<IEnumerable<DocenteConCursosDto>> ObtenerDocentesConCursosActivosAsync();
        Task<ResultadoBatchHorariosDto> CrearHorariosBatchAsync(CrearHorariosBatchDto dto);
        Task<int> EliminarTodosHorariosAsync();
    }
}
