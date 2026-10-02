using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaPracticas.api.Models
{
    public class Libro
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Autor { get; set; }

        public string? Descripcion { get; set; }

        [Required]
        public string RutaArchivo { get; set; } = string.Empty;

        public string? RutaPortada { get; set; }

        public string? PaginasPreview { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        public int CategoriaId { get; set; }
        [ForeignKey(nameof(CategoriaId))]
        public Categoria? Categoria { get; set; }

        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }
        public string? TablaContenido { get; set; } // JSON con array de {titulo, pagina}
        public int ContadorDescargas { get; set; } = 0;
        public int? NumeroPaginas { get; set; }
        public long? TamanoBytes { get; set; }
        public List<Semestre> Semestres { get; set; } = new();
    }
}