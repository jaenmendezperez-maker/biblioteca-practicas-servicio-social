using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null)
                return Unauthorized("Email o contraseña incorrectos.");

            if (!usuario.Activo)
                return Unauthorized("Esta cuenta ha sido desactivada.");

            bool passwordValido = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordValido)
                return Unauthorized("Email o contraseña incorrectos.");

            var token = GenerarToken(usuario.Id, usuario.Email, usuario.Rol.ToString());

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol.ToString()
            });
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
    }
}