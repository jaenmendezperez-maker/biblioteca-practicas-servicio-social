using BibliotecaPracticas.api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPracticas.api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
    }
}