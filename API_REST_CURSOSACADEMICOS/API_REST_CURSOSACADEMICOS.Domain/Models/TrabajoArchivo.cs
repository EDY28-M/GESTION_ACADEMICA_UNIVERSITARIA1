using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("TrabajoArchivo")]
    public class TrabajoArchivo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idTrabajo")]
        public int IdTrabajo { get; set; }

        [Required]
        [StringLength(500)]
        [Column("nombreArchivo")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Column("rutaArchivo")]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("tipoArchivo")]
        public string? TipoArchivo { get; set; }

        [Column("tamaño")]
        public long? Tamaño { get; set; }

        [Required]
        [Column("fechaSubida")]
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("IdTrabajo")]
        public virtual TrabajoEncargado? Trabajo { get; set; }
    }
}

