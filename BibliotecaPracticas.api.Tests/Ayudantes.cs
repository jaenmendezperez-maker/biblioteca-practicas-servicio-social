using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.Models;
using BibliotecaPracticas.api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace BibliotecaPracticas.api.Tests;

public static class Ayudantes
{
    public static AppDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opciones);
    }

    public static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-de-prueba-con-al-menos-32-caracteres-123456",
                ["Jwt:Issuer"] = "Pruebas",
                ["Jwt:Audience"] = "Pruebas",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

    public static Usuario CrearUsuario(
        string email = "prueba@alumno.buap.mx",
        bool verificado = true,
        bool activo = true,
        string password = "Prueba123") =>
        new Usuario
        {
            NombreCompleto = "Usuario de prueba",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = RolUsuario.Alumno,
            FechaRegistro = DateTime.UtcNow,
            Activo = activo,
            EmailVerificado = verificado
        };

    /// Crea un usuario sin verificar con un código conocido.
    public static Usuario CrearPendiente(
        AppDbContext db,
        string codigo = "123456",
        int minutosParaVencer = 10,
        int intentos = 0,
        int minutosDesdeUltimoEnvio = 2)
    {
        var u = CrearUsuario(verificado: false);
        u.CodigoVerificacionHash = BCrypt.Net.BCrypt.HashPassword(codigo);
        u.CodigoExpira = DateTime.UtcNow.AddMinutes(minutosParaVencer);
        u.IntentosCodigo = intentos;
        u.UltimoEnvioCodigo = DateTime.UtcNow.AddMinutes(-minutosDesdeUltimoEnvio);
        db.Usuarios.Add(u);
        db.SaveChanges();
        return u;
    }
}

/// Correo de mentira: guarda lo que se "envió" en lugar de mandarlo.
public class CorreoFalso : ICorreoService
{
    public List<(string Destino, string Codigo)> Enviados { get; } = new();
    public List<(string Destino, string Codigo)> Recuperaciones { get; } = new();
    public bool Fallar { get; set; }

    public Task EnviarCodigoAsync(string destino, string nombre, string codigo)
    {
        if (Fallar) throw new InvalidOperationException("SMTP caído");
        Enviados.Add((destino, codigo));
        return Task.CompletedTask;
    }

    public Task EnviarRecuperacionAsync(string destino, string nombre, string codigo)
    {
        if (Fallar) throw new InvalidOperationException("SMTP caído");
        Recuperaciones.Add((destino, codigo));
        return Task.CompletedTask;
    }
}

/// Entorno web mínimo para probar controladores que leen archivos.
public class EntornoFalso : IWebHostEnvironment
{
    public EntornoFalso(string raiz)
    {
        ContentRootPath = raiz;
        WebRootPath = Path.Combine(raiz, "wwwroot");
    }

    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "Pruebas";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Development";
}