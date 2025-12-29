using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Docente")]
    public class Docente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("idUsuario")]
        public int? IdUsuario { get; set; }

        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Profesion { get; set; }

        [Column("fecha_nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(100)]
        public string? Correo { get; set; }

        [Column("password_hash")]
        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relación con Cursos
        public virtual ICollection<Curso> Cursos { get; set; } = new List<Curso>();
    }
}
