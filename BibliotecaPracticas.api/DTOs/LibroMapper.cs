using System.Text.Json;
using BibliotecaPracticas.api.Models;

namespace BibliotecaPracticas.api.DTOs
{
    public static class LibroMapper
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        public static LibroDTO ToDto(Libro l) => new LibroDTO
        {
            Id = l.Id,
            Titulo = l.Titulo,
            Autor = l.Autor,
            Descripcion = l.Descripcion,
            RutaArchivo = l.RutaArchivo,
            RutaPortada = l.RutaPortada,
            PaginasPreview = l.PaginasPreview != null
                ? JsonSerializer.Deserialize<List<string>>(l.PaginasPreview, _opts) : null,
            TablaContenido = l.TablaContenido != null
                ? JsonSerializer.Deserialize<List<ItemTablaContenidoDTO>>(l.TablaContenido, _opts) : null,
            FechaSubida = l.FechaSubida,
            CategoriaId = l.CategoriaId,
            CategoriaNombre = l.Categoria?.Nombre,
            Activo = l.Activo,
            ContadorDescargas = l.ContadorDescargas,
            NumeroPaginas = l.NumeroPaginas,
            TamanoBytes = l.TamanoBytes,
            Semestres = l.Semestres
                .OrderBy(s => s.Numero)
                .Select(s => new SemestreDTO { Id = s.Id, Numero = s.Numero, Nombre = s.Nombre })
                .ToList()
        };
    }
}