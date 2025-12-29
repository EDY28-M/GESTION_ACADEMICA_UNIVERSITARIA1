using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    public class HorarioDto
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public string NombreDocente { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public string DiaSemanaTexto { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty; // Formato HH:mm
        public string HoraFin { get; set; } = string.Empty;    // Formato HH:mm
        public string? Aula { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public class CrearHorarioDto
    {
        [Required]
        public int IdCurso { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo)")]
        public int DiaSemana { get; set; }

        [Required]
        public string HoraInicio { get; set; } = string.Empty; // Recibimos string "HH:mm" y lo convertimos

        [Required]
        public string HoraFin { get; set; } = string.Empty;

        public string? Aula { get; set; }

        [Required]
        public string Tipo { get; set; } = string.Empty;
    }

    public class HorarioConflictoDto
    {
        public bool HayConflicto { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string CursoConflicto { get; set; } = string.Empty;
        public string HorarioConflicto { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para representar un curso con sus horarios asignados
    /// </summary>
    public class CursoConHorariosDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public int Ciclo { get; set; }
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public List<HorarioDto> Horarios { get; set; } = new List<HorarioDto>();
    }

    /// <summary>
    /// DTO para representar un docente con todos sus cursos activos y sus horarios
    /// </summary>
    public class DocenteConCursosDto
    {
        public int IdDocente { get; set; }
        public string NombreDocente { get; set; } = string.Empty;
        public string Profesion { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public List<CursoConHorariosDto> Cursos { get; set; } = new List<CursoConHorariosDto>();
        public int TotalCursos => Cursos.Count;
        public int TotalHorariosAsignados => Cursos.Sum(c => c.Horarios.Count);
    }

    /// <summary>
    /// DTO para crear múltiples horarios de una vez (batch)
    /// </summary>
    public class CrearHorariosBatchDto
    {
        [Required]
        public int IdDocente { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe incluir al menos un horario")]
        public List<CrearHorarioDto> Horarios { get; set; } = new List<CrearHorarioDto>();
    }

    /// <summary>
    /// DTO para respuesta de creación batch
    /// </summary>
    public class ResultadoBatchHorariosDto
    {
        public int TotalEnviados { get; set; }
        public int TotalCreados { get; set; }
        public int TotalFallidos { get; set; }
        public List<HorarioDto> HorariosCreados { get; set; } = new List<HorarioDto>();
        public List<ErrorHorarioDto> Errores { get; set; } = new List<ErrorHorarioDto>();
    }

    /// <summary>
    /// DTO para errores individuales en creación batch
    /// </summary>
    public class ErrorHorarioDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int DiaSemana { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
