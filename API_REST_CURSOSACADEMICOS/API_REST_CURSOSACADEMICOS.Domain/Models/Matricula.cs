using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Matricula")]
    public class Matricula
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idEstudiante")]
        public int IdEstudiante { get; set; }

        [Column("idCurso")]
        public int IdCurso { get; set; }

        [Column("idPeriodo")]
        public int IdPeriodo { get; set; }

        [Column("fecha_matricula")]
        public DateTime FechaMatricula { get; set; } = DateTime.Now;

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "Matriculado";

        [Column("fecha_retiro")]
        public DateTime? FechaRetiro { get; set; }

        [Column("isAutorizado")]
        public bool IsAutorizado { get; set; } = false;

        [Column("promedio_final")]
        public decimal? PromedioFinal { get; set; }

        [Column("observaciones")]
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("IdEstudiante")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("IdCurso")]
        public virtual Curso? Curso { get; set; }

        [ForeignKey("IdPeriodo")]
        public virtual Periodo? Periodo { get; set; }

        public virtual ICollection<Nota> Notas { get; set; } = new List<Nota>();
    }
}
