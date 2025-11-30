using System.ComponentModel.DataAnnotations;

namespace API_REST_CURSOSACADEMICOS.DTOs
{
    // ============================================
    // DTOs BÁSICOS DE CURSO
    // ============================================

    /// <summary>
    /// DTO para mostrar información completa de un curso
    /// </summary>
    public class CursoDto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public int? HorasTeoria { get; set; }
        public int? HorasPractica { get; set; }
        public int? HorasTotales { get; set; }
        public int Ciclo { get; set; }
        public string? Semestre { get; set; } // "I" o "II"
        public int? IdDocente { get; set; }
        public DocenteSimpleDto? Docente { get; set; }
        public List<int> PrerequisitosIds { get; set; } = new List<int>();
        public List<CursoSimpleDto> Prerequisitos { get; set; } = new List<CursoSimpleDto>();
    }

    /// <summary>
    /// DTO para crear un nuevo curso
    /// </summary>
    public class CursoCreateDto
    {
        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreCurso { get; set; } = string.Empty;

        [Required]
        [Range(1, 10)]
        public int Creditos { get; set; }

        [Required]
        [Range(1, 40)]
        public int HorasSemanal { get; set; }

        [Range(0, 30)]
        public int? HorasTeoria { get; set; }

        [Range(0, 30)]
        public int? HorasPractica { get; set; }

        [Range(0, 200)]
        public int? HorasTotales { get; set; }

        [Required]
        [Range(1, 10)]
        public int Ciclo { get; set; }

        [StringLength(5)]
        public string? Semestre { get; set; } // "I" o "II"

        public int? IdDocente { get; set; }

        public List<int> PrerequisitosIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// DTO para actualizar un curso existente
    /// </summary>
    public class CursoUpdateDto
    {
        [StringLength(20)]
        public string? Codigo { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreCurso { get; set; } = string.Empty;

        [Required]
        [Range(1, 10)]
        public int Creditos { get; set; }

        [Required]
        [Range(1, 40)]
        public int HorasSemanal { get; set; }

        [Range(0, 30)]
        public int? HorasTeoria { get; set; }

        [Range(0, 30)]
        public int? HorasPractica { get; set; }

        [Range(0, 200)]
        public int? HorasTotales { get; set; }

        [Required]
        [Range(1, 10)]
        public int Ciclo { get; set; }

        [StringLength(5)]
        public string? Semestre { get; set; } // "I" o "II"

        public int? IdDocente { get; set; }

        public List<int> PrerequisitosIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// DTO para cursos disponibles para matrícula
    /// </summary>
    public class CursoDisponibleDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCurso { get; set; } = string.Empty;
        public int Creditos { get; set; }
        public int HorasSemanal { get; set; }
        public int Ciclo { get; set; }
        public string? NombreDocente { get; set; }
        public bool Disponible { get; set; }
        public bool YaMatriculado { get; set; }
        public string? MotivoNoDisponible { get; set; }
        public int EstudiantesMatriculados { get; set; }
        public int? CapacidadMaxima { get; set; }
    }

    /// <summary>
    /// DTO simplificado de docente para cursos
    /// </summary>
    public class DocenteSimpleDto
    {
        public int Id { get; set; }
        public string Apellidos { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string? Profesion { get; set; }
    }
}
