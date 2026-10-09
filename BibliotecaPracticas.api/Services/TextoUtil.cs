using System.Globalization;
using System.Text;

namespace BibliotecaPracticas.api.Services
{
    public static class TextoUtil
    {
        /// Quita espacios al inicio y al final y junta los espacios repetidos.
        public static string Limpiar(string? texto) =>
            string.Join(' ', (texto ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        /// Texto para comparar: sin acentos, sin mayúsculas, sin espacios de más.
        public static string Clave(string? texto)
        {
            var descompuesto = Limpiar(texto).Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}