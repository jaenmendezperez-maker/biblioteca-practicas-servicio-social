using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemestresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SemestresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/semestres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemestreConConteoDTO>>> GetSemestres()
        {
            var data = await _context.Semestres
                .OrderBy(s => s.Numero)
                .Select(s => new SemestreConConteoDTO
                {
                    Id = s.Id,
                    Numero = s.Numero,
                    Nombre = s.Nombre,
                    TotalLibros = s.Libros.Count(l => l.Activo && l.Categoria != null && l.Categoria.Activo)
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/semestres/3/libros
        [HttpGet("{id}/libros")]
        public async Task<ActionResult<IEnumerable<LibroDTO>>> GetLibrosDeSemestre(int id)
        {
            var libros = await _context.Libros
                .Where(l => l.Activo && l.Categoria != null && l.Categoria.Activo
                            && l.Semestres.Any(s => s.Id == id))
                .Include(l => l.Categoria)
                .Include(l => l.Semestres)
                .ToListAsync();

            return Ok(libros.Select(LibroMapper.ToDto).ToList());
        }
    }
}