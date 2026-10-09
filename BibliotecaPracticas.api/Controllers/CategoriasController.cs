using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.Models;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Services;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/categorias  (solo activas, para el catálogo público)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new CategoriaDTO { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: api/categorias/todas  (incluye inactivas, para el panel de admin)
        [HttpGet("todas")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetTodasLasCategorias()
        {
            var categorias = await _context.Categorias
                .Select(c => new CategoriaDTO { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: api/categorias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaDTO>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);

            if (categoria == null)
                return NotFound();

            return Ok(new CategoriaDTO { Id = categoria.Id, Nombre = categoria.Nombre, Activo = categoria.Activo });
        }

        // POST: api/categorias
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoriaDTO>> PostCategoria(CategoriaCreateDTO dto)
        {
            var (nombre, error) = await ValidarNombreAsync(dto.Nombre, idActual: null);
            if (error != null)
                return BadRequest(error);

            var categoria = new Categoria { Nombre = nombre, Activo = true };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            var result = new CategoriaDTO { Id = categoria.Id, Nombre = categoria.Nombre, Activo = categoria.Activo };
            return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id }, result);
        }

        // PUT: api/categorias/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCategoria(int id, CategoriaCreateDTO dto)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            var (nombre, error) = await ValidarNombreAsync(dto.Nombre, idActual: id);
            if (error != null)
                return BadRequest(error);

            categoria.Nombre = nombre;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/categorias/5/estado  (activar/desactivar)
        [HttpPatch("{id}/estado")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            categoria.Activo = activo;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// Limpia el nombre y revisa que no exista ya (también entre las dadas de baja).
        private async Task<(string Nombre, string? Error)> ValidarNombreAsync(string? crudo, int? idActual)
        {
            var nombre = TextoUtil.Limpiar(crudo);
            if (nombre.Length == 0)
                return (nombre, "El nombre de la categoría es obligatorio.");

            var clave = TextoUtil.Clave(nombre);

            // La tabla es pequeña: se compara en memoria para poder ignorar acentos.
            var existentes = await _context.Categorias
                .Where(c => idActual == null || c.Id != idActual)
                .Select(c => new { c.Nombre, c.Activo })
                .ToListAsync();

            var duplicada = existentes.FirstOrDefault(c => TextoUtil.Clave(c.Nombre) == clave);
            if (duplicada == null)
                return (nombre, null);

            return (nombre, duplicada.Activo
                ? $"Ya existe una categoría llamada \"{duplicada.Nombre}\"."
                : $"Ya existe una categoría llamada \"{duplicada.Nombre}\", pero está dada de baja. Reactívala en lugar de crear otra.");
        }
    }
}