using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.Models;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/usuarios  (todos, incluye inactivos — solo para el panel de admin)
        [HttpGet]
        [Authorize]
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
        [Authorize]
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

        // POST: api/usuarios  (registro público — siempre crea como Alumno)
        [HttpPost]
        public async Task<ActionResult<UsuarioDTO>> PostUsuario(UsuarioCreateDTO dto)
        {
            var existe = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
            if (existe)
                return BadRequest("Ya existe un usuario con ese email.");

            var usuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Rol = RolUsuario.Alumno, // Forzado: nadie puede auto-asignarse Admin/Docente
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var result = new UsuarioDTO
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Rol = usuario.Rol,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo
            };

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, result);
        }

        // PATCH: api/usuarios/5/rol
        [HttpPatch("{id}/rol")]
        [Authorize]
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
        [Authorize]
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