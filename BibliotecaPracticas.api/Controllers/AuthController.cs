using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BibliotecaPracticas.api.Services;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICorreoService _correo;

        public AuthController(AppDbContext context, IConfiguration config, ICorreoService correo)
        {
            _context = context;
            _config = config;
            _correo = correo;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
        {
            var emailLogin = dto.Email?.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == emailLogin);

            if (usuario == null)
                return Unauthorized("Email o contraseña incorrectos.");

            if (!usuario.Activo)
                return Unauthorized("Esta cuenta ha sido desactivada.");

            bool passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordValido)
                return Unauthorized("Email o contraseña incorrectos.");
            
            if (!usuario.EmailVerificado)
                return StatusCode(403, "Debes verificar tu correo antes de iniciar sesión.");

            var token = GenerarToken(usuario.Id, usuario.Email, usuario.Rol.ToString());

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol.ToString()
            });
        }

        // POST: api/auth/verificar-correo
        [HttpPost("verificar-correo")]
        public async Task<ActionResult<AuthResponseDTO>> VerificarCorreo(VerificarCorreoDTO dto)
        {
            const string mensajeGenerico = "Código incorrecto o vencido.";
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || usuario.EmailVerificado
                || usuario.CodigoVerificacionHash == null
                || usuario.CodigoExpira == null
                || usuario.CodigoExpira < DateTime.UtcNow)
                return BadRequest(mensajeGenerico);

            if (usuario.IntentosCodigo >= 5)
                return BadRequest("Demasiados intentos. Pide un nuevo código.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Codigo.Trim(), usuario.CodigoVerificacionHash))
            {
                usuario.IntentosCodigo++;
                await _context.SaveChangesAsync();
                return BadRequest(mensajeGenerico);
            }

            usuario.EmailVerificado = true;
            usuario.CodigoVerificacionHash = null;
            usuario.CodigoExpira = null;
            usuario.IntentosCodigo = 0;
            await _context.SaveChangesAsync();

            var token = GenerarToken(usuario.Id, usuario.Email, usuario.Rol.ToString());

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol.ToString()
            });
        }

        // POST: api/auth/reenviar-codigo
        [HttpPost("reenviar-codigo")]
        public async Task<IActionResult> ReenviarCodigo(ReenviarCodigoDTO dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario != null && !usuario.EmailVerificado)
            {
                if (usuario.UltimoEnvioCodigo > DateTime.UtcNow.AddSeconds(-60))
                    return BadRequest("Espera un minuto antes de pedir otro código.");

                var codigo = CodigoVerificacion.Generar();
                usuario.CodigoVerificacionHash = BCrypt.Net.BCrypt.HashPassword(codigo);
                usuario.CodigoExpira = DateTime.UtcNow.AddMinutes(10);
                usuario.IntentosCodigo = 0;
                usuario.UltimoEnvioCodigo = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                try
                {
                    await _correo.EnviarCodigoAsync(usuario.Email, usuario.NombreCompleto, codigo);
                }
                catch
                {
                    return StatusCode(500, "No se pudo enviar el correo. Intenta más tarde.");
                }
            }

            // Misma respuesta exista o no el correo, para no revelar quién está registrado
            return Ok(new { mensaje = "Si el correo está pendiente de verificación, se envió un nuevo código." });
        }
        // GET: api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<AuthResponseDTO>> Me()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (usuarioIdClaim == null || !int.TryParse(usuarioIdClaim, out int usuarioId))
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
                return NotFound();

            return Ok(new
            {
                id = usuario.Id,
                email = usuario.Email,
                nombreCompleto = usuario.NombreCompleto,
                rol = usuario.Rol.ToString()
            });
        }

        private string GenerarToken(int usuarioId, string email, string rol)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiresInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        // POST: api/auth/solicitar-recuperacion
        [HttpPost("solicitar-recuperacion")]
        public async Task<IActionResult> SolicitarRecuperacion(SolicitarRecuperacionDTO dto)
        {
            const string respuesta = "Si el correo está registrado, te enviamos un código para restablecer tu contraseña.";
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            // Misma respuesta exista o no la cuenta, esté activa o en espera: no se revela quién está registrado.
            if (usuario == null || !usuario.Activo || !usuario.EmailVerificado
                || usuario.UltimoEnvioRecuperacion > DateTime.UtcNow.AddSeconds(-60))
                return Ok(new { mensaje = respuesta });

            var codigo = CodigoVerificacion.Generar();
            usuario.CodigoRecuperacionHash = BCrypt.Net.BCrypt.HashPassword(codigo);
            usuario.CodigoRecuperacionExpira = DateTime.UtcNow.AddMinutes(10);
            usuario.IntentosRecuperacion = 0;
            usuario.UltimoEnvioRecuperacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            try
            {
                await _correo.EnviarRecuperacionAsync(usuario.Email, usuario.NombreCompleto, codigo);
            }
            catch
            {
                return StatusCode(500, "No se pudo enviar el correo. Intenta más tarde.");
            }

            return Ok(new { mensaje = respuesta });
        }

        // POST: api/auth/restablecer-password
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword(RestablecerPasswordDTO dto)
        {
            const string mensajeGenerico = "Código incorrecto o vencido.";

            if (string.IsNullOrEmpty(dto.NuevaPassword) || dto.NuevaPassword.Length < 6)
                return BadRequest("La contraseña debe tener al menos 6 caracteres.");

            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || !usuario.Activo
                || usuario.CodigoRecuperacionHash == null
                || usuario.CodigoRecuperacionExpira == null
                || usuario.CodigoRecuperacionExpira < DateTime.UtcNow)
                return BadRequest(mensajeGenerico);

            if (usuario.IntentosRecuperacion >= 5)
                return BadRequest("Demasiados intentos. Pide un nuevo código.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Codigo.Trim(), usuario.CodigoRecuperacionHash))
            {
                usuario.IntentosRecuperacion++;
                await _context.SaveChangesAsync();
                return BadRequest(mensajeGenerico);
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.CodigoRecuperacionHash = null;
            usuario.CodigoRecuperacionExpira = null;
            usuario.IntentosRecuperacion = 0;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
        }
    }
}