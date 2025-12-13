namespace API_REST_CURSOSACADEMICOS.DTOs
{
    /// <summary>
    /// DTO para mostrar información de un período académico
    /// </summary>
    public class PeriodoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Ciclo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// DTO para crear un nuevo período académico
    /// </summary>
    public class CrearPeriodoDto
    {
        public string Nombre { get; set; } = string.Empty; // Ej: "2025-I"
        public int Anio { get; set; } // Ej: 2025
        public string Ciclo { get; set; } = string.Empty; // Ej: "I" o "II"
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activo { get; set; } = false;
    }

    /// <summary>
    /// DTO para editar un período académico existente
    /// </summary>
    public class EditarPeriodoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Ciclo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}
