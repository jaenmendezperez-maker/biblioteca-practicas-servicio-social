namespace BibliotecaPracticas.api.Models
{
    public class Semestre
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<Libro> Libros { get; set; } = new();
    }
}