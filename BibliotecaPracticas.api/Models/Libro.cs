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
        public string RutaArchivo { get; set; } = string.Empty; // ruta del PDF guardado

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        // Relación con Categoria
        public int CategoriaId { get; set; }
        [ForeignKey(nameof(CategoriaId))]
        public Categoria? Categoria { get; set; }

        // Relación con Usuario (quién lo subió)
        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }
    }
}