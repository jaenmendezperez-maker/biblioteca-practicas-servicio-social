using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PermitirAngular")]
    public class ArchivosController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ArchivosController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("{carpeta}/{nombreArchivo}")]
        public IActionResult ObtenerArchivo(string carpeta, string nombreArchivo, [FromQuery] bool descargar = false)
        {
            var carpetasPermitidas = new[] { "libros", "portadas", "preview" };
            if (!carpetasPermitidas.Contains(carpeta))
                return BadRequest();

            var rutaCompleta = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", carpeta, nombreArchivo);

            if (!System.IO.File.Exists(rutaCompleta))
                return NotFound();

            var contentType = nombreArchivo.EndsWith(".pdf") ? "application/pdf" : "image/png";

            if (descargar)
            {
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{nombreArchivo}\"");
            }

            return PhysicalFile(rutaCompleta, contentType, enableRangeProcessing: false);
        }
    }
}