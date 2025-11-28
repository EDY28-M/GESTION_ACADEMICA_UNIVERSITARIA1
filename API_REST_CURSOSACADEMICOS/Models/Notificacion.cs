using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Notificacion")]
    public class Notificacion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("tipo")]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [Column("accion")]
        [MaxLength(50)]
        public string Accion { get; set; } = string.Empty;

        [Required]
        [Column("mensaje")]
        [MaxLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Column("metadataJson")]
        [MaxLength(1000)]
        public string? MetadataJson { get; set; }

        [Column("idUsuario")]
        public int? IdUsuario { get; set; }

        [Column("fechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("leida")]
        public bool Leida { get; set; } = false;

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}
