namespace BibliotecaPracticas.api.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
    public class VerificarCorreoDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }

    public class ReenviarCodigoDTO
    {
    public string Email { get; set; } = string.Empty;
    }
        public class SolicitarRecuperacionDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RestablecerPasswordDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }
}