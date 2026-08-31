using Microsoft.AspNetCore.Http;

namespace BibliotecaPracticas.api.DTOs
{
    public class LibroCreateDTO
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Autor { get; set; }
        public string? Descripcion { get; set; }
        public int CategoriaId { get; set; }
        public int UsuarioId { get; set; }
        public IFormFile Archivo { get; set; } = null!;
        public IFormFile? Portada { get; set; }
        public List<IFormFile>? Paginas { get; set; }
        public string? TablaContenidoJson { get; set; }
    }
}