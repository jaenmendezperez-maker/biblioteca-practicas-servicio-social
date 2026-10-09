using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.Models;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Authorization;
using BibliotecaPracticas.api.Services;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICorreoService _correo;

        public UsuariosController(AppDbContext context, ICorreoService correo)
        {
            _context = context;
            _correo = correo;
        }

        // GET: api/usuarios  (todos, incluye inactivos — solo para el panel de admin)
        [HttpGet]
        [Authorize(Roles = "Admin")] // Solo los administradores pueden ver la lista completa de usuarios
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioDTO
                {
                    Id = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email,
                    Rol = u.Rol,
                    FechaRegistro = u.FechaRegistro,
                    Activo = u.Activo
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // GET: api/usuarios/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsuarioDTO>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
                return NotFound();

            return Ok(new UsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo
            });
        }
        private static readonly string[] DominiosPermitidos =
        {
            "alumno.buap.mx",
            "correo.buap.mx",
            "buap.mx"
        };
        // POST: api/usuarios  (registro público — crea como Alumno, pendiente de verificar)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostUsuario(UsuarioCreateDTO dto)
        {
            var email = dto.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            var partes = email.Split('@');
            if (partes.Length != 2
                || partes[0].Length == 0
                || !DominiosPermitidos.Contains(partes[1]))
                return BadRequest("Solo se permiten correos institucionales de la BUAP (@alumno.buap.mx, @correo.buap.mx o @buap.mx).");

            var existente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (existente != null)
            {
                return BadRequest(existente.EmailVerificado
                    ? "Ya existe un usuario con ese email."
                    : "Este correo ya se registró pero falta verificarlo. Pide un nuevo código.");
            }

            var codigo = CodigoVerificacion.Generar();

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = RolUsuario.Alumno,
                FechaRegistro = DateTime.UtcNow,
                Activo = true,
                EmailVerificado = false,
                CodigoVerificacionHash = BCrypt.Net.BCrypt.HashPassword(codigo),
                CodigoExpira = DateTime.UtcNow.AddMinutes(10),
                IntentosCodigo = 0,
                UltimoEnvioCodigo = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            try
            {
                await _correo.EnviarCodigoAsync(usuario.Email, usuario.NombreCompleto, codigo);
            }
            catch
            {
                return StatusCode(500, "Tu cuenta se creó, pero no se pudo enviar el correo. Pide un nuevo código en unos minutos.");
            }

            return Ok(new { email = usuario.Email, requiereVerificacion = true });
        }

        // PATCH: api/usuarios/5/rol
        [HttpPatch("{id}/rol")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarRol(int id, UsuarioEditRolDTO dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.Rol = dto.Rol;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/usuarios/5/estado
        [HttpPatch("{id}/estado")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            // Evita que un usuario se desactive a sí mismo, sin importar desde dónde venga la petición
            var usuarioIdActual = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdActual != null && int.TryParse(usuarioIdActual, out int idActual) && idActual == id && !activo)
            {
                return BadRequest("No puedes desactivar tu propia cuenta.");
            }

            usuario.Activo = activo;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}