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
        public bool EmailVerificado { get; set; }
        public string? CodigoVerificacionHash { get; set; }
        public DateTime? CodigoExpira { get; set; }
        public int IntentosCodigo { get; set; }
        public DateTime? UltimoEnvioCodigo { get; set; }
        public string? CodigoRecuperacionHash { get; set; }
        public DateTime? CodigoRecuperacionExpira { get; set; }
        public int IntentosRecuperacion { get; set; }
        public DateTime? UltimoEnvioRecuperacion { get; set; }
    }

    public enum RolUsuario
    {
        Admin,
        Docente,
        Alumno
    }
}