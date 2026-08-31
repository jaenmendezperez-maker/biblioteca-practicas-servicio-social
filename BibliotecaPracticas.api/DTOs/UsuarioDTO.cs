using BibliotecaPracticas.api.Models;

namespace BibliotecaPracticas.api.DTOs
{
    public class UsuarioDTO
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public RolUsuario Rol { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
    }

    public class UsuarioCreateDTO
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public RolUsuario Rol { get; set; } = RolUsuario.Alumno;
    }

    public class UsuarioEditRolDTO
    {
        public RolUsuario Rol { get; set; }
    }
}