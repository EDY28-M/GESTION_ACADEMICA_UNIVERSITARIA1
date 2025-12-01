using System;
using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    public class HorarioDto
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; }
        public string NombreDocente { get; set; }
        public int DiaSemana { get; set; }
        public string DiaSemanaTexto { get; set; }
        public string HoraInicio { get; set; } // Formato HH:mm
        public string HoraFin { get; set; }    // Formato HH:mm
        public string? Aula { get; set; }
        public string Tipo { get; set; }
    }

    public class CrearHorarioDto
    {
        [Required]
        public int IdCurso { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "El día de la semana debe estar entre 1 (Lunes) y 7 (Domingo)")]
        public int DiaSemana { get; set; }

        [Required]
        public string HoraInicio { get; set; } // Recibimos string "HH:mm" y lo convertimos

        [Required]
        public string HoraFin { get; set; }

        public string? Aula { get; set; }

        [Required]
        public string Tipo { get; set; }
    }

    public class HorarioConflictoDto
    {
        public bool HayConflicto { get; set; }
        public string Mensaje { get; set; }
        public string CursoConflicto { get; set; }
        public string HorarioConflicto { get; set; }
    }
}
