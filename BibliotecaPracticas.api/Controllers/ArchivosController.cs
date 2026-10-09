using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace BibliotecaPracticas.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("PermitirAngular")]
    public class ArchivosController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ArchivosController> _logger;

        public ArchivosController(IWebHostEnvironment env, ILogger<ArchivosController> logger)
        {
            _env = env;
            _logger = logger;
        }

        [HttpGet("{carpeta}/{nombreArchivo}")]
        public IActionResult ObtenerArchivo(string carpeta, string nombreArchivo, [FromQuery] bool descargar = false)
        {
            var carpetasPermitidas = new[] { "libros", "portadas", "preview" };
            if (!carpetasPermitidas.Contains(carpeta))
                return BadRequest();
            
            if (carpeta == "libros" && User.Identity?.IsAuthenticated != true)
                return Unauthorized("Debes iniciar sesión para ver o descargar este libro.");

            nombreArchivo = Path.GetFileName(nombreArchivo); // Evitar rutas relativas
                
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
        // GET: api/archivos/libros/{nombreArchivo}/fragmento
        // Entrega solo las primeras páginas (25 %, mínimo 5). Siempre se deja al menos una página fuera.
        [HttpGet("libros/{nombreArchivo}/fragmento")]
        public IActionResult ObtenerFragmento(string nombreArchivo)
        {
            nombreArchivo = Path.GetFileName(nombreArchivo);
            if (!nombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest();

            var origen = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "libros", nombreArchivo);
            if (!System.IO.File.Exists(origen))
                return NotFound();

            var carpeta = Path.Combine(_env.ContentRootPath, "fragmentos");
            Directory.CreateDirectory(carpeta);
            var destino = Path.Combine(carpeta, nombreArchivo);
            var datos = destino + ".txt"; // guarda "total;incluidas" para no reabrir el PDF original

            try
            {
                if (!System.IO.File.Exists(destino) || !System.IO.File.Exists(datos))
                {
                    using var original = PdfReader.Open(origen, PdfDocumentOpenMode.Import);
                    var total = original.PageCount;
                    var incluidas = Math.Max(1, Math.Min(
                        Math.Max(5, (int)Math.Ceiling(total * 0.25)),
                        total - 1));

                    using var fragmento = new PdfDocument();
                    for (int i = 0; i < incluidas; i++)
                        fragmento.AddPage(original.Pages[i]);

                    var temporal = destino + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    fragmento.Save(temporal);
                    System.IO.File.Move(temporal, destino, overwrite: true);
                    System.IO.File.WriteAllText(datos, $"{total};{incluidas}");
                }

                var partes = System.IO.File.ReadAllText(datos).Split(';');
                Response.Headers["X-Total-Paginas"] = partes[0];
                Response.Headers["X-Paginas-Fragmento"] = partes[1];

                return PhysicalFile(destino, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar el fragmento de {Archivo}", nombreArchivo);
                return StatusCode(500, "No se pudo generar la vista parcial de este libro.");
            }
        }
    }
}