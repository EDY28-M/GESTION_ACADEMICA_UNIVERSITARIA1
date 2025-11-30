using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Curso")]
    public class Curso
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(20)]
        [Column("codigo")]
        public string? Codigo { get; set; }

        [Required]
        [StringLength(200)]
        [Column("curso")]
        public string NombreCurso { get; set; } = string.Empty;

        [Required]
        public int Creditos { get; set; }

        [Required]
        public int HorasSemanal { get; set; }

        [Column("horasTeoria")]
        public int? HorasTeoria { get; set; }

        [Column("horasPractica")]
        public int? HorasPractica { get; set; }

        [Column("horasTotales")]
        public int? HorasTotales { get; set; }

        [Required]
        public int Ciclo { get; set; }

        [StringLength(5)]
        [Column("semestre")]
        public string? Semestre { get; set; } // "I" o "II"

        // Foreign Key
        [Column("idDocente")]
        public int? IdDocente { get; set; }

        // Navigation Property
        [ForeignKey("IdDocente")]
        public virtual Docente? Docente { get; set; }

        // Relación con prerequisitos
        public virtual ICollection<CursoPrerequisito> PrerequisitosRequeridos { get; set; } = new List<CursoPrerequisito>();

        public virtual ICollection<CursoPrerequisito> EsPrerrequisitoDE { get; set; } = new List<CursoPrerequisito>();
    }
}
