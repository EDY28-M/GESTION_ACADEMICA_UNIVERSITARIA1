using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Horario")]
    public class Horario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idCurso")]
        public int IdCurso { get; set; }

        [Column("dia_semana")]
        public int DiaSemana { get; set; } // 1=Lunes, 2=Martes, etc.

        [Column("hora_inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Column("hora_fin")]
        public TimeSpan HoraFin { get; set; }

        [Column("aula")]
        [StringLength(50)]
        public string? Aula { get; set; }

        [Column("tipo")]
        [StringLength(20)]
        public string Tipo { get; set; } = "Teoría"; // Teoría, Práctica

        [ForeignKey("IdCurso")]
        public virtual Curso Curso { get; set; }
    }
}
