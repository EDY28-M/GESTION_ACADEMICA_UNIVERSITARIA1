using System;
using System.ComponentModel.DataAnnotations;

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
}
