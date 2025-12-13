using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_REST_CURSOSACADEMICOS.Models
{
    [Table("CursoPrerequisito")]
    public class CursoPrerequisito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("idCurso")]
        public int IdCurso { get; set; }

        [Required]
        [Column("idCursoPrerequisito")]
        public int IdCursoPrerequisito { get; set; }

        // Navigation Properties
        [ForeignKey("IdCurso")]
        public virtual Curso CursoDestino { get; set; } = null!;

        [ForeignKey("IdCursoPrerequisito")]
        public virtual Curso Prerequisito { get; set; } = null!;
    }
}
