namespace API_REST_CURSOSACADEMICOS.Models
{
    public class TipoEvaluacion
    {
        public int Id { get; set; }
        public int IdCurso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Peso { get; set; } // Porcentaje (0-100)
        public int Orden { get; set; } // Para mantener el orden de visualización
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual Curso? Curso { get; set; }
    }
}
