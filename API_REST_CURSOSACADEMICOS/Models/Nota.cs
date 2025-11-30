using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Nota")]
    public class Nota
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idMatricula")]
        public int IdMatricula { get; set; }

        [Required]
        [Column("tipo_evaluacion")]
        [MaxLength(50)]
        public string TipoEvaluacion { get; set; } = string.Empty;

        [Column("nota")]
        [Range(0, 20)]
        public decimal NotaValor { get; set; }

        [Column("peso")]
        [Range(0, 100)]
        public decimal Peso { get; set; }

        [Column("fecha_evaluacion")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("observaciones")]
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("IdMatricula")]
        public virtual Matricula? Matricula { get; set; }
    }
}
