namespace API_REST_CURSOSACADEMICOS.DTOs
{
    public class TipoEvaluacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class CrearTipoEvaluacionDto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public int Orden { get; set; }
    }

    public class ActualizarTipoEvaluacionDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class ConfigurarTiposEvaluacionDto
    {
        public List<ActualizarTipoEvaluacionDto> TiposEvaluacion { get; set; } = new List<ActualizarTipoEvaluacionDto>();
    }
}
