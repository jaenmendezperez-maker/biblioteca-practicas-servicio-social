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
        public DbSet<Semestre> Semestres { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Semestre>().HasData(
                Enumerable.Range(1, 10).Select(n => new Semestre
                {
                    Id = n,
                    Numero = n,
                    Nombre = $"Semestre {n}"
                })
            );
        }
    }
}