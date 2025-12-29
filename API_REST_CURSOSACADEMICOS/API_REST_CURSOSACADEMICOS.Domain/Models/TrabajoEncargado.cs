using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("TrabajoEncargado")]
    public class TrabajoEncargado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idCurso")]
        public int IdCurso { get; set; }

        [Required]
        [Column("idDocente")]
        public int IdDocente { get; set; }

        [Required]
        [StringLength(200)]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("descripcion", TypeName = "nvarchar(MAX)")]
        public string? Descripcion { get; set; }

        [Required]
        [Column("fechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("fechaLimite")]
        public DateTime FechaLimite { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fechaActualizacion")]
        public DateTime? FechaActualizacion { get; set; }

        [Column("idTipoEvaluacion")]
        public int? IdTipoEvaluacion { get; set; }

        [Column("numeroTrabajo")]
        public int? NumeroTrabajo { get; set; } // Número del trabajo dentro de la serie (1, 2, 3, etc.)

        [Column("totalTrabajos")]
        public int? TotalTrabajos { get; set; } // Total de trabajos en la serie para este tipo de evaluación

        [Column("pesoIndividual", TypeName = "decimal(5,2)")]
        public decimal? PesoIndividual { get; set; } // Peso individual de este trabajo (calculado automáticamente)

        // Navigation Properties
        [ForeignKey("IdCurso")]
        public virtual Curso? Curso { get; set; }

        [ForeignKey("IdDocente")]
        public virtual Docente? Docente { get; set; }

        [ForeignKey("IdTipoEvaluacion")]
        public virtual TipoEvaluacion? TipoEvaluacion { get; set; }

        // Relaciones con archivos y links
        public virtual ICollection<TrabajoArchivo> Archivos { get; set; } = new List<TrabajoArchivo>();
        public virtual ICollection<TrabajoLink> Links { get; set; } = new List<TrabajoLink>();
        public virtual ICollection<TrabajoEntrega> Entregas { get; set; } = new List<TrabajoEntrega>();
    }
}

