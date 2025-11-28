using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("Estudiante")]
    public class Estudiante
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("codigo")]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Column("apellidos")]
        [MaxLength(100)]
        public string? Apellidos { get; set; }

        [Column("nombres")]
        [MaxLength(100)]
        public string? Nombres { get; set; }

        [Column("dni")]
        [MaxLength(20)]
        public string? Dni { get; set; }

        [Column("fecha_nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Column("correo")]
        [MaxLength(100)]
        public string? Correo { get; set; }

        [Column("telefono")]
        [MaxLength(20)]
        public string? Telefono { get; set; }

        [Column("direccion")]
        [MaxLength(200)]
        public string? Direccion { get; set; }

        [Column("ciclo_actual")]
        public int CicloActual { get; set; } = 1;

        [Column("creditos_aprobados")]
        public int CreditosAcumulados { get; set; } = 0;

        [Column("promedio_acumulado")]
        public decimal? PromedioAcumulado { get; set; }

        [Column("promedio_semestral")]
        public decimal? PromedioSemestral { get; set; }

        [Column("id_periodo_ultimo")]
        public int? IdPeriodoUltimo { get; set; }

        [Required]
        [Column("carrera")]
        [MaxLength(100)]
        public string Carrera { get; set; } = "Ingeniería de Sistemas";

        [Column("fechaIngreso")]
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        [Column("promocion")]
        [MaxLength(10)]
        public string? Promocion { get; set; }

        [Column("total_creditos_llevados")]
        public int TotalCreditosLlevados { get; set; } = 0;

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "Activo";

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }

        public virtual ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();
    }
}
