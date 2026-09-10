using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaPracticas.api.Models
{
    public class Favorito
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        public int LibroId { get; set; }
        [ForeignKey(nameof(LibroId))]
        public Libro? Libro { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;
    }
}