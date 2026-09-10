using System.Security.Claims;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BibliotecaPracticas.api.DTOs;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritosController(AppDbContext context)
        {
            _context = context;
        }
        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        // GET: api/favoritos  (lista de IDs de libros favoritos del usuario actual)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<int>>> GetFavoritos()
        {
            var usuarioId = ObtenerUsuarioId();

            var libroIds = await _context.Favoritos
                .Where(f => f.UsuarioId == usuarioId)
                .Select(f => f.LibroId)
                .ToListAsync();

            return Ok(libroIds);
        }

        // POST: api/favoritos/5  (agrega el libro 5 a favoritos)
        [HttpPost("{libroId}")]
        public async Task<IActionResult> AgregarFavorito(int libroId)
        {
            var usuarioId = ObtenerUsuarioId();

            var yaExiste = await _context.Favoritos
                .AnyAsync(f => f.UsuarioId == usuarioId && f.LibroId == libroId);

            if (yaExiste)
                return NoContent();

            _context.Favoritos.Add(new Favorito
            {
                UsuarioId = usuarioId,
                LibroId = libroId,
                FechaAgregado = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/favoritos/5  (quita el libro 5 de favoritos)
        [HttpDelete("{libroId}")]
        public async Task<IActionResult> QuitarFavorito(int libroId)
        {
            var usuarioId = ObtenerUsuarioId();

            var favorito = await _context.Favoritos
                .FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.LibroId == libroId);

            if (favorito == null)
                return NotFound();

            _context.Favoritos.Remove(favorito);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/favoritos/libros  (los libros completos guardados como favoritos)
        [HttpGet("libros")]
        public async Task<ActionResult<IEnumerable<LibroDTO>>> GetLibrosFavoritos()
        {
            var usuarioId = ObtenerUsuarioId();

            var librosRaw = await _context.Favoritos
                .Where(f => f.UsuarioId == usuarioId)
                .Include(f => f.Libro)
                .ThenInclude(l => l!.Categoria)
                .OrderByDescending(f => f.FechaAgregado)
                .Select(f => f.Libro)
                .ToListAsync();

            var libros = librosRaw
                .Where(l => l != null && l.Activo)
                .Select(l => new LibroDTO
                {
                    Id = l!.Id,
                    Titulo = l.Titulo,
                    Autor = l.Autor,
                    Descripcion = l.Descripcion,
                    RutaArchivo = l.RutaArchivo,
                    RutaPortada = l.RutaPortada,
                    PaginasPreview = l.PaginasPreview != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(l.PaginasPreview, _jsonOptions)
                        : null,
                    TablaContenido = l.TablaContenido != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<ItemTablaContenidoDTO>>(l.TablaContenido, _jsonOptions)
                        : null,
                    FechaSubida = l.FechaSubida,
                    CategoriaId = l.CategoriaId,
                    CategoriaNombre = l.Categoria != null ? l.Categoria.Nombre : null,
                    Activo = l.Activo
                }).ToList();

            return Ok(libros);
        }
    }
}