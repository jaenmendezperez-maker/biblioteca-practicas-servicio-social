using BibliotecaPracticas.api.Controllers;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPracticas.api.Tests;

public class RegistroTests
{
    private static (UsuariosController c, AppDbContext db, CorreoFalso correo) Crear()
    {
        var db = Ayudantes.CrearContexto();
        var correo = new CorreoFalso();
        return (new UsuariosController(db, correo), db, correo);
    }

    private static UsuarioCreateDTO Datos(string email) => new()
    {
        NombreCompleto = "Prueba",
        Email = email,
        Password = "Prueba123"
    };

    [Theory]
    [InlineData("alguien@gmail.com")]
    [InlineData("alguien@buap.com")]
    [InlineData("alguien@alumno.buap.mx.falso.com")]
    [InlineData("alguien@evil-buap.mx")]
    public async Task Rechaza_correos_que_no_son_de_la_BUAP(string email)
    {
        var (c, db, correo) = Crear();

        var r = await c.PostUsuario(Datos(email));

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Empty(db.Usuarios);
        Assert.Empty(correo.Enviados);
    }

    // Estas dos pueden FALLAR con el código actual: el dominio solo se revisa con EndsWith.
    [Theory]
    [InlineData("a@b@buap.mx")]
    [InlineData("@buap.mx")]
    public async Task Rechaza_correos_mal_formados_aunque_terminen_en_buap(string email)
    {
        var (c, db, _) = Crear();

        var r = await c.PostUsuario(Datos(email));

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Empty(db.Usuarios);
    }

    [Theory]
    [InlineData("a@alumno.buap.mx")]
    [InlineData("a@correo.buap.mx")]
    [InlineData("a@buap.mx")]
    public async Task Acepta_los_tres_dominios_institucionales(string email)
    {
        var (c, db, _) = Crear();

        var r = await c.PostUsuario(Datos(email));

        Assert.IsType<OkObjectResult>(r);
        Assert.Single(db.Usuarios);
    }

    [Fact]
    public async Task Crea_la_cuenta_sin_verificar_y_manda_el_codigo()
    {
        var (c, db, correo) = Crear();

        var r = await c.PostUsuario(Datos("  Prueba@Alumno.BUAP.mx "));

        Assert.IsType<OkObjectResult>(r);
        var u = Assert.Single(db.Usuarios);
        Assert.Equal("prueba@alumno.buap.mx", u.Email);          // limpia espacios y mayúsculas
        Assert.False(u.EmailVerificado);
        Assert.Equal(RolUsuario.Alumno, u.Rol);
        Assert.NotEqual("Prueba123", u.PasswordHash);             // la contraseña no se guarda en texto plano
        Assert.True(u.CodigoExpira > DateTime.UtcNow.AddMinutes(9));

        var envio = Assert.Single(correo.Enviados);
        Assert.Equal("prueba@alumno.buap.mx", envio.Destino);
        Assert.Matches(@"^\d{6}$", envio.Codigo);                 // 6 dígitos
        Assert.NotEqual(envio.Codigo, u.CodigoVerificacionHash);  // el código se guarda cifrado
        Assert.True(BCrypt.Net.BCrypt.Verify(envio.Codigo, u.CodigoVerificacionHash!));
    }

    [Fact]
    public async Task Rechaza_un_correo_ya_registrado_y_verificado()
    {
        var (c, db, _) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario(verificado: true));
        db.SaveChanges();

        var r = await c.PostUsuario(Datos("prueba@alumno.buap.mx"));

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Contains("Ya existe", (string)bad.Value!);
        Assert.Single(db.Usuarios);
    }

    [Fact]
    public async Task Avisa_si_el_correo_esta_registrado_pero_sin_verificar()
    {
        var (c, db, _) = Crear();
        Ayudantes.CrearPendiente(db);

        var r = await c.PostUsuario(Datos("prueba@alumno.buap.mx"));

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Contains("falta verificarlo", (string)bad.Value!);
    }

    [Fact]
    public async Task Si_falla_el_envio_responde_500_pero_la_cuenta_queda_creada()
    {
        var (c, db, correo) = Crear();
        correo.Fallar = true;

        var r = await c.PostUsuario(Datos("prueba@alumno.buap.mx"));

        var obj = Assert.IsType<ObjectResult>(r);
        Assert.Equal(500, obj.StatusCode);
        Assert.Single(db.Usuarios);
    }
}