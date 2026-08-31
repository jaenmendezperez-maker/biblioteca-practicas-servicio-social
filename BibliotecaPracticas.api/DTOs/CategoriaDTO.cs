namespace BibliotecaPracticas.api.DTOs
{
    public class CategoriaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class CategoriaCreateDTO
    {
        public string Nombre { get; set; } = string.Empty;
    }
}