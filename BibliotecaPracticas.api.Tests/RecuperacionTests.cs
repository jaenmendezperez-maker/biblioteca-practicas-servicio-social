using BibliotecaPracticas.api.Controllers;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPracticas.api.Tests;

public class RecuperacionTests
{
    private static (AuthController c, AppDbContext db, CorreoFalso correo) Crear()
    {
        var db = Ayudantes.CrearContexto();
        var correo = new CorreoFalso();
        return (new AuthController(db, Ayudantes.CrearConfiguracion(), correo), db, correo);
    }

    private static Usuario ConCodigo(
        AppDbContext db,
        string codigo = "123456",
        int minutosParaVencer = 10,
        int intentos = 0)
    {
        var u = Ayudantes.CrearUsuario();
        u.CodigoRecuperacionHash = BCrypt.Net.BCrypt.HashPassword(codigo);
        u.CodigoRecuperacionExpira = DateTime.UtcNow.AddMinutes(minutosParaVencer);
        u.IntentosRecuperacion = intentos;
        db.Usuarios.Add(u);
        db.SaveChanges();
        return u;
    }

    private static RestablecerPasswordDTO Datos(string email, string codigo, string nueva = "NuevaClave1") =>
        new() { Email = email, Codigo = codigo, NuevaPassword = nueva };

    // ---------- Solicitar ----------

    [Fact]
    public async Task Solicitar_manda_un_codigo_a_una_cuenta_verificada()
    {
        var (c, db, correo) = Crear();
        var u = Ayudantes.CrearUsuario();
        db.Usuarios.Add(u);
        db.SaveChanges();

        var r = await c.SolicitarRecuperacion(new SolicitarRecuperacionDTO { Email = "  PRUEBA@alumno.buap.mx " });

        Assert.IsType<OkObjectResult>(r);
        var envio = Assert.Single(correo.Recuperaciones);
        Assert.Equal("prueba@alumno.buap.mx", envio.Destino);
        Assert.Matches(@"^\d{6}$", envio.Codigo);
        Assert.True(BCrypt.Net.BCrypt.Verify(envio.Codigo, u.CodigoRecuperacionHash!));
        Assert.True(u.CodigoRecuperacionExpira > DateTime.UtcNow.AddMinutes(9));
    }

    [Fact]
    public async Task Solicitar_a_un_correo_desconocido_responde_igual_y_no_envia_nada()
    {
        var (c, _, correo) = Crear();

        var r = await c.SolicitarRecuperacion(new SolicitarRecuperacionDTO { Email = "nadie@alumno.buap.mx" });

        Assert.IsType<OkObjectResult>(r);
        Assert.Empty(correo.Recuperaciones);
    }

    [Fact]
    public async Task Solicitar_no_envia_nada_a_cuentas_sin_verificar_ni_desactivadas()
    {
        var (c, db, correo) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario("a@alumno.buap.mx", verificado: false));
        db.Usuarios.Add(Ayudantes.CrearUsuario("b@alumno.buap.mx", activo: false));
        db.SaveChanges();

        await c.SolicitarRecuperacion(new SolicitarRecuperacionDTO { Email = "a@alumno.buap.mx" });
        await c.SolicitarRecuperacion(new SolicitarRecuperacionDTO { Email = "b@alumno.buap.mx" });

        Assert.Empty(correo.Recuperaciones);
    }

    [Fact]
    public async Task Solicitar_dos_veces_seguidas_solo_envia_un_codigo()
    {
        var (c, db, correo) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario());
        db.SaveChanges();
        var datos = new SolicitarRecuperacionDTO { Email = "prueba@alumno.buap.mx" };

        await c.SolicitarRecuperacion(datos);
        var segunda = await c.SolicitarRecuperacion(datos);

        Assert.IsType<OkObjectResult>(segunda);
        Assert.Single(correo.Recuperaciones);
    }

    [Fact]
    public async Task Si_falla_el_envio_responde_500()
    {
        var (c, db, correo) = Crear();
        correo.Fallar = true;
        db.Usuarios.Add(Ayudantes.CrearUsuario());
        db.SaveChanges();

        var r = await c.SolicitarRecuperacion(new SolicitarRecuperacionDTO { Email = "prueba@alumno.buap.mx" });

        Assert.Equal(500, Assert.IsType<ObjectResult>(r).StatusCode);
    }

    // ---------- Restablecer ----------

    [Fact]
    public async Task Con_el_codigo_correcto_cambia_la_contrasena_y_permite_entrar()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456");

        var r = await c.RestablecerPassword(Datos(u.Email, "123456", "NuevaClave1"));

        Assert.IsType<OkObjectResult>(r);
        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave1", u.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Prueba123", u.PasswordHash));
        Assert.Null(u.CodigoRecuperacionHash);

        var login = await c.Login(new LoginDTO { Email = u.Email, Password = "NuevaClave1" });
        Assert.IsType<OkObjectResult>(login.Result);
    }

    [Fact]
    public async Task Con_codigo_incorrecto_no_cambia_nada_y_cuenta_el_intento()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456");
        var hashAntes = u.PasswordHash;

        var r = await c.RestablecerPassword(Datos(u.Email, "000000"));

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal(hashAntes, u.PasswordHash);
        Assert.Equal(1, u.IntentosRecuperacion);
    }

    [Fact]
    public async Task Con_codigo_vencido_no_cambia_la_contrasena()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456", minutosParaVencer: -1);

        var r = await c.RestablecerPassword(Datos(u.Email, "123456"));

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.True(BCrypt.Net.BCrypt.Verify("Prueba123", u.PasswordHash));
    }

    [Fact]
    public async Task Despues_de_5_intentos_ni_el_codigo_correcto_sirve()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456", intentos: 5);

        var r = await c.RestablecerPassword(Datos(u.Email, "123456"));

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Contains("Demasiados", (string)bad.Value!);
        Assert.True(BCrypt.Net.BCrypt.Verify("Prueba123", u.PasswordHash));
    }

    [Fact]
    public async Task El_codigo_no_se_puede_usar_dos_veces()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456");

        await c.RestablecerPassword(Datos(u.Email, "123456", "NuevaClave1"));
        var segunda = await c.RestablecerPassword(Datos(u.Email, "123456", "OtraClave99"));

        Assert.IsType<BadRequestObjectResult>(segunda);
        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave1", u.PasswordHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    public async Task Rechaza_contrasenas_demasiado_cortas_y_conserva_el_codigo(string nueva)
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456");

        var r = await c.RestablecerPassword(Datos(u.Email, "123456", nueva));

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.True(BCrypt.Net.BCrypt.Verify("Prueba123", u.PasswordHash));
        Assert.NotNull(u.CodigoRecuperacionHash);
    }

    [Fact]
    public async Task Un_correo_desconocido_recibe_el_mismo_mensaje_generico()
    {
        var (c, _, _) = Crear();

        var r = await c.RestablecerPassword(Datos("nadie@alumno.buap.mx", "123456"));

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Código incorrecto o vencido.", bad.Value);
    }

    [Fact]
    public async Task Una_cuenta_desactivada_no_puede_restablecer()
    {
        var (c, db, _) = Crear();
        var u = ConCodigo(db, "123456");
        u.Activo = false;
        db.SaveChanges();

        var r = await c.RestablecerPassword(Datos(u.Email, "123456"));

        Assert.IsType<BadRequestObjectResult>(r);
    }
}