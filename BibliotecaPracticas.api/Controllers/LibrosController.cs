using System.Text.Json;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibrosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LibrosController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/libros
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LibroDTO>>> GetLibros()
        {
            var libros = await _context.Libros
                .Where(l => l.Activo && l.Categoria != null && l.Categoria.Activo)
                .Include(l => l.Categoria)
                .Include(l => l.Semestres)
                .ToListAsync();

            return Ok(libros.Select(LibroMapper.ToDto).ToList());
        }

        // GET: api/libros/todos
        [HttpGet("todos")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<LibroDTO>>> GetTodosLosLibros()
        {
            var libros = await _context.Libros
                .Include(l => l.Categoria)
                .Include(l => l.Semestres)
                .ToListAsync();

            return Ok(libros.Select(LibroMapper.ToDto).ToList());
        }

        // GET: api/libros/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LibroDTO>> GetLibro(int id)
        {
            var libro = await _context.Libros
                .Include(l => l.Categoria)
                .Include(l => l.Semestres)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (libro == null)
                return NotFound();

            return Ok(LibroMapper.ToDto(libro));
        }

        // POST: api/libros
        [HttpPost]
        [Authorize]
        [RequestSizeLimit(150 * 1024 * 1024)]
        public async Task<ActionResult<LibroDTO>> PostLibro([FromForm] LibroCreateDTO dto)
        {
            if (dto.Archivo == null || dto.Archivo.Length == 0)
                return BadRequest("Debes adjuntar un archivo PDF.");

            if (Path.GetExtension(dto.Archivo.FileName).ToLower() != ".pdf")
                return BadRequest("Solo se permiten archivos PDF.");

            if (dto.Archivo.Length > 150 * 1024 * 1024)
                return BadRequest("El archivo no debe superar los 150MB.");

            var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(dto.Archivo.FileName)}";
            var carpetaDestino = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "libros");

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await dto.Archivo.CopyToAsync(stream);
            }

            string? rutaPortadaGuardada = null;

            if (dto.Portada != null && dto.Portada.Length > 0)
            {
                var nombrePortada = $"{Guid.NewGuid()}.png";
                var carpetaPortadas = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "portadas");

                if (!Directory.Exists(carpetaPortadas))
                    Directory.CreateDirectory(carpetaPortadas);

                var rutaPortadaCompleta = Path.Combine(carpetaPortadas, nombrePortada);

                using (var streamPortada = new FileStream(rutaPortadaCompleta, FileMode.Create))
                {
                    await dto.Portada.CopyToAsync(streamPortada);
                }

                rutaPortadaGuardada = $"/uploads/portadas/{nombrePortada}";
            }

            List<string> rutasPaginas = new();

            if (dto.Paginas != null && dto.Paginas.Count > 0)
            {
                var carpetaPreview = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "preview");
                if (!Directory.Exists(carpetaPreview))
                    Directory.CreateDirectory(carpetaPreview);

                foreach (var pagina in dto.Paginas)
                {
                    var nombrePagina = $"{Guid.NewGuid()}.png";
                    var rutaPaginaCompleta = Path.Combine(carpetaPreview, nombrePagina);

                    using (var streamPagina = new FileStream(rutaPaginaCompleta, FileMode.Create))
                    {
                        await pagina.CopyToAsync(streamPagina);
                    }

                    rutasPaginas.Add($"/uploads/preview/{nombrePagina}");
                }
            }

            var libro = new Libro
            {
                Titulo = dto.Titulo,
                Autor = dto.Autor,
                Descripcion = dto.Descripcion,
                RutaArchivo = $"/uploads/libros/{nombreArchivo}",
                RutaPortada = rutaPortadaGuardada,
                CategoriaId = dto.CategoriaId,
                UsuarioId = dto.UsuarioId,
                FechaSubida = DateTime.UtcNow,
                Activo = true,
                PaginasPreview = rutasPaginas.Count > 0 ? JsonSerializer.Serialize(rutasPaginas) : null,
                TablaContenido = !string.IsNullOrWhiteSpace(dto.TablaContenidoJson) ? dto.TablaContenidoJson : null,
                NumeroPaginas = dto.NumeroPaginas,
                TamanoBytes = dto.Archivo.Length,
            };

            if (dto.SemestreIds != null && dto.SemestreIds.Count > 0)
            {
                libro.Semestres = await _context.Semestres
                    .Where(s => dto.SemestreIds.Contains(s.Id))
                    .ToListAsync();
            }

            _context.Libros.Add(libro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLibro), new { id = libro.Id }, LibroMapper.ToDto(libro));
        }

        // PUT: api/libros/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutLibro(int id, [FromBody] LibroEditDTO dto)
        {
            var libro = await _context.Libros
                .Include(l => l.Semestres)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (libro == null)
                return NotFound();

            libro.Titulo = dto.Titulo;
            libro.Autor = dto.Autor;
            libro.Descripcion = dto.Descripcion;
            libro.CategoriaId = dto.CategoriaId;

            // null = no tocar los semestres; lista (incluso vacía) = reemplazarlos
            if (dto.SemestreIds != null)
            {
                libro.Semestres.Clear();
                var nuevos = await _context.Semestres
                    .Where(s => dto.SemestreIds.Contains(s.Id))
                    .ToListAsync();
                foreach (var s in nuevos) libro.Semestres.Add(s);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/libros/5/estado
        [HttpPatch("{id}/estado")]
        [Authorize]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool activo)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound();

            libro.Activo = activo;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ===== Reprocesamiento de libros antiguos =====

        public class MetadatosExtraidosDTO
        {
            public string? RutaPortada { get; set; }
            public List<string>? PaginasPreview { get; set; }
            public string? TablaContenidoJson { get; set; }
        }

        // PATCH: api/libros/5/metadatos-extraidos
        [HttpPatch("{id}/metadatos-extraidos")]
        [Authorize]
        public async Task<IActionResult> ActualizarMetadatosExtraidos(int id, [FromBody] MetadatosExtraidosDTO dto)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.RutaPortada) && string.IsNullOrWhiteSpace(libro.RutaPortada))
                libro.RutaPortada = dto.RutaPortada;

            if (dto.PaginasPreview != null && dto.PaginasPreview.Count > 0 && string.IsNullOrWhiteSpace(libro.PaginasPreview))
                libro.PaginasPreview = JsonSerializer.Serialize(dto.PaginasPreview);

            if (!string.IsNullOrWhiteSpace(dto.TablaContenidoJson) && string.IsNullOrWhiteSpace(libro.TablaContenido))
                libro.TablaContenido = dto.TablaContenidoJson;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/libros/5/subir-imagen-extraida
        [HttpPost("{id}/subir-imagen-extraida")]
        [Authorize]
        public async Task<ActionResult<string>> SubirImagenExtraida(int id, [FromForm] ImagenExtraidaDTO dto)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound();

            var carpeta = dto.Tipo == "portada" ? "portadas" : "preview";
            var carpetaDestino = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", carpeta);

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"{Guid.NewGuid()}.png";
            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await dto.Imagen.CopyToAsync(stream);
            }

            return Ok(new { ruta = $"/uploads/{carpeta}/{nombreArchivo}" });
        }

        // POST: api/libros/5/registrar-descarga
        [HttpPost("{id}/registrar-descarga")]
        public async Task<IActionResult> RegistrarDescarga(int id)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound();

            libro.ContadorDescargas++;
            await _context.SaveChangesAsync();

            return Ok(libro.ContadorDescargas);
        }

        // PUT: api/libros/5/reemplazar-pdf
        [HttpPut("{id}/reemplazar-pdf")]
        [Authorize]
        [RequestSizeLimit(150 * 1024 * 1024)]
        public async Task<IActionResult> ReemplazarPdf(int id, [FromForm] ReemplazarPdfDTO dto)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound();

            if (dto.Archivo == null || dto.Archivo.Length == 0)
                return BadRequest("Debes adjuntar un archivo PDF.");

            if (Path.GetExtension(dto.Archivo.FileName).ToLower() != ".pdf")
                return BadRequest("Solo se permiten archivos PDF.");

            if (dto.Archivo.Length > 150 * 1024 * 1024)
                return BadRequest("El archivo no debe superar los 150MB.");

            // Borra el PDF anterior del disco
            var rutaAnterior = Path.Combine(_env.ContentRootPath, "wwwroot", libro.RutaArchivo.TrimStart('/'));
            if (System.IO.File.Exists(rutaAnterior))
                System.IO.File.Delete(rutaAnterior);

            // Guarda el nuevo PDF
            var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(dto.Archivo.FileName)}";
            var carpetaDestino = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "libros");

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await dto.Archivo.CopyToAsync(stream);
            }

            libro.RutaArchivo = $"/uploads/libros/{nombreArchivo}";

            // Limpia los datos derivados del PDF anterior (ya no corresponden al nuevo archivo)
            libro.RutaPortada = null;
            libro.PaginasPreview = null;
            libro.TablaContenido = null;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}