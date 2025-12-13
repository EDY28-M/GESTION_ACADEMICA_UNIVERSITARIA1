using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    /// <summary>
    /// Modelo para tokens de recuperación de contraseña
    /// </summary>
    [Table("PasswordResetToken")]
    public class PasswordResetToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("token")]
        [StringLength(100)]
        public string Token { get; set; } = string.Empty;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_expiracion")]
        public DateTime FechaExpiracion { get; set; }

        [Column("usado")]
        public bool Usado { get; set; } = false;

        [Required]
        [Column("tipo_usuario")]
        [StringLength(20)]
        public string TipoUsuario { get; set; } = string.Empty; // "Usuario", "Docente", "Estudiante"

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("id_docente")]
        public int? IdDocente { get; set; }

        [Column("id_estudiante")]
        public int? IdEstudiante { get; set; }
    }
}
