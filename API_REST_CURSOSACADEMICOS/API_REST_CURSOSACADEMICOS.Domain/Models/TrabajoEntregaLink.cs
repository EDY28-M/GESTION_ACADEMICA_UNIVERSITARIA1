using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("TrabajoEntregaLink")]
    public class TrabajoEntregaLink
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idEntrega")]
        public int IdEntrega { get; set; }

        [Required]
        [StringLength(500)]
        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [StringLength(200)]
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Required]
        [Column("fechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("IdEntrega")]
        public virtual TrabajoEntrega? Entrega { get; set; }
    }
}

