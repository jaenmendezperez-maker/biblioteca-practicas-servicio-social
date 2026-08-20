using System.ComponentModel.DataAnnotations;

namespace BibliotecaPracticas.api.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty; // Ej: "Ingeniería en Sistemas"

        public ICollection<Libro>? Libros { get; set; }
    }
}