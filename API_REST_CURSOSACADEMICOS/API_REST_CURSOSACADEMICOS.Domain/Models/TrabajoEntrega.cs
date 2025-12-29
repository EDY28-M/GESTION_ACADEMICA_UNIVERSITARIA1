using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("TrabajoEntrega")]
    public class TrabajoEntrega
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idTrabajo")]
        public int IdTrabajo { get; set; }

        [Required]
        [Column("idEstudiante")]
        public int IdEstudiante { get; set; }

        [Column("comentario", TypeName = "nvarchar(MAX)")]
        public string? Comentario { get; set; }

        [Required]
        [Column("fechaEntrega")]
        public DateTime FechaEntrega { get; set; } = DateTime.UtcNow;

        [Column("calificacion")]
        public decimal? Calificacion { get; set; }

        [Column("observaciones", TypeName = "nvarchar(MAX)")]
        public string? Observaciones { get; set; }

        [Column("fechaCalificacion")]
        public DateTime? FechaCalificacion { get; set; }

        [Column("entregadoTarde")]
        public bool EntregadoTarde { get; set; } = false;

        // Navigation Properties
        [ForeignKey("IdTrabajo")]
        public virtual TrabajoEncargado? Trabajo { get; set; }

        [ForeignKey("IdEstudiante")]
        public virtual Estudiante? Estudiante { get; set; }

        // Relaciones con archivos y links de entrega
        public virtual ICollection<TrabajoEntregaArchivo> Archivos { get; set; } = new List<TrabajoEntregaArchivo>();
        public virtual ICollection<TrabajoEntregaLink> Links { get; set; } = new List<TrabajoEntregaLink>();
    }
}

