namespace BibliotecaPracticas.api.DTOs
{
    public class SemestreDTO
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class SemestreConConteoDTO
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int TotalLibros { get; set; }
    }
}