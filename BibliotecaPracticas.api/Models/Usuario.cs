using System.ComponentModel.DataAnnotations;

namespace BibliotecaPracticas.api.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public RolUsuario Rol { get; set; } = RolUsuario.Alumno;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relación: un usuario puede subir varios libros
        public ICollection<Libro>? Libros { get; set; }
        public bool Activo { get; set; } = true;
    }

    public enum RolUsuario
    {
        Admin,
        Docente,
        Alumno
    }
}