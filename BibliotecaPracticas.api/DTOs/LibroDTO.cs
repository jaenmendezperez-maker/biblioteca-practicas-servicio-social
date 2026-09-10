namespace BibliotecaPracticas.api.DTOs
{
    public class ItemTablaContenidoDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public int? Pagina { get; set; }
    }

    public class LibroDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Autor { get; set; }
        public string? Descripcion { get; set; }
        public string RutaArchivo { get; set; } = string.Empty;
        public string? RutaPortada { get; set; }
        public List<string>? PaginasPreview { get; set; }
        public List<ItemTablaContenidoDTO>? TablaContenido { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaSubida { get; set; }
        public int CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public int ContadorDescargas { get; set; }
    }
}