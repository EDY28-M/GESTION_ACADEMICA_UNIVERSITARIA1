using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Asistencia")]
    public class Asistencia
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idEstudiante")]
        public int IdEstudiante { get; set; }

        [Required]
        [Column("idCurso")]
        public int IdCurso { get; set; }

        [Required]
        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Required]
        [Column("presente")]
        public bool Presente { get; set; }

        [Column("observaciones")]
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required]
        [Column("tipoClase")]
        [MaxLength(20)]
        public string TipoClase { get; set; } = "Teoría"; // "Teoría" o "Práctica"

        // Navegación
        [ForeignKey("IdEstudiante")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("IdCurso")]
        public virtual Curso? Curso { get; set; }
    }
}
