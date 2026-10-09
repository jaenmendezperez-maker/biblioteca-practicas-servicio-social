using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace BibliotecaPracticas.api.Services
{
    public interface ICorreoService
    {
        Task EnviarCodigoAsync(string destino, string nombre, string codigo);
        Task EnviarRecuperacionAsync(string destino, string nombre, string codigo);
    }

    public static class CodigoVerificacion
    {
        public static string Generar() =>
            RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    public class CorreoService : ICorreoService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IConfiguration config, ILogger<CorreoService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task EnviarCodigoAsync(string destino, string nombre, string codigo) =>
            EnviarAsync(destino, "Tu código de verificación",
                $"Hola {nombre},\n\nTu código de verificación es: {codigo}\n\n" +
                "Vence en 10 minutos. Si no creaste una cuenta en la Biblioteca de Prácticas, ignora este mensaje.",
                $"[MODO CONSOLA] Código de verificación para {destino}: {codigo}");

        public Task EnviarRecuperacionAsync(string destino, string nombre, string codigo) =>
            EnviarAsync(destino, "Recupera tu contraseña",
                $"Hola {nombre},\n\nTu código para restablecer la contraseña es: {codigo}\n\n" +
                "Vence en 10 minutos. Si no lo solicitaste, ignora este mensaje: tu contraseña no cambiará.",
                $"[MODO CONSOLA] Código de recuperación para {destino}: {codigo}");

        private async Task EnviarAsync(string destino, string asunto, string cuerpo, string textoConsola)
        {
            var seccion = _config.GetSection("Correo");

            if (seccion.GetValue<bool>("ModoConsola"))
            {
                _logger.LogWarning("{Texto}", textoConsola);
                return;
            }

            var servidor = seccion["Servidor"];
            var usuario = seccion["Usuario"];
            var clave = seccion["Clave"];

            if (string.IsNullOrWhiteSpace(servidor) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                _logger.LogError("Configuración de correo incompleta: revisa Servidor, Usuario y Clave en la sección \"Correo\".");
                throw new InvalidOperationException("Configuración de correo incompleta.");
            }

            var correoRemitente = string.IsNullOrWhiteSpace(seccion["Remitente"]) ? usuario : seccion["Remitente"]!;
            var nombreRemitente = seccion["NombreRemitente"] ?? "Biblioteca de Prácticas";

            using var mensaje = new MailMessage
            {
                From = new MailAddress(correoRemitente, nombreRemitente),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };
            mensaje.To.Add(destino);

            using var smtp = new SmtpClient(servidor, seccion.GetValue<int?>("Puerto") ?? 587)
            {
                Credentials = new NetworkCredential(usuario, clave),
                EnableSsl = true
            };

            try
            {
                await smtp.SendMailAsync(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo a {Destino}", destino);
                throw;
            }
        }
    }
}