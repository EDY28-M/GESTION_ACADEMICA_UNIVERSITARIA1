namespace API_REST_CURSOSACADEMICOS.DTOs
{
    public class NotificacionDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public object? Metadata { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Leida { get; set; }
    }

    public class NotificacionCreateDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public object? Metadata { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class NotificacionMarkReadDto
    {
        public List<int> NotificacionIds { get; set; } = new();
    }
}
