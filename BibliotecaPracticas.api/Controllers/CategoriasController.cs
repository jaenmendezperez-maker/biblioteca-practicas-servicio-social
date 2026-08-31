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
        [Authorize]
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
        [Authorize]
        public async Task<ActionResult<CategoriaDTO>> PostCategoria(CategoriaCreateDTO dto)
        {
            var categoria = new Categoria { Nombre = dto.Nombre, Activo = true };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            var result = new CategoriaDTO { Id = categoria.Id, Nombre = categoria.Nombre, Activo = categoria.Activo };
            return CreatedAtAction(nameof(GetCategoria), new { id = categoria.Id }, result);
        }

        // PUT: api/categorias/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCategoria(int id, CategoriaCreateDTO dto)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            categoria.Nombre = dto.Nombre;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/categorias/5/estado  (activar/desactivar)
        [HttpPatch("{id}/estado")]
        [Authorize]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            categoria.Activo = activo;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}