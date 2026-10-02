namespace BibliotecaPracticas.api.DTOs
{
    public class LibroEditDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Autor { get; set; }
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public List<int>? SemestreIds { get; set; }
    }
}