using BibliotecaPracticas.api.Controllers;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPracticas.api.Tests;

public class AuthTests
{
    private static (AuthController c, AppDbContext db, CorreoFalso correo) Crear()
    {
        var db = Ayudantes.CrearContexto();
        var correo = new CorreoFalso();
        return (new AuthController(db, Ayudantes.CrearConfiguracion(), correo), db, correo);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_correcto_devuelve_token()
    {
        var (c, db, _) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario());
        db.SaveChanges();

        var r = await c.Login(new LoginDTO { Email = "prueba@alumno.buap.mx", Password = "Prueba123" });

        var ok = Assert.IsType<OkObjectResult>(r.Result);
        var dto = Assert.IsType<AuthResponseDTO>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.Token));
        Assert.Equal("Alumno", dto.Rol);
    }

    [Fact]
    public async Task Login_con_contrasena_incorrecta_es_401()
    {
        var (c, db, _) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario());
        db.SaveChanges();

        var r = await c.Login(new LoginDTO { Email = "prueba@alumno.buap.mx", Password = "otra" });

        Assert.IsType<UnauthorizedObjectResult>(r.Result);
    }

    [Fact]
    public async Task Login_con_correo_inexistente_es_401()
    {
        var (c, _, _) = Crear();

        var r = await c.Login(new LoginDTO { Email = "nadie@alumno.buap.mx", Password = "x" });

        Assert.IsType<UnauthorizedObjectResult>(r.Result);
    }

    [Fact]
    public async Task Login_de_cuenta_desactivada_es_401()
    {
        var (c, db, _) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario(activo: false));
        db.SaveChanges();

        var r = await c.Login(new LoginDTO { Email = "prueba@alumno.buap.mx", Password = "Prueba123" });

        Assert.IsType<UnauthorizedObjectResult>(r.Result);
    }

    [Fact]
    public async Task Login_sin_verificar_el_correo_es_403()
    {
        var (c, db, _) = Crear();
        Ayudantes.CrearPendiente(db);

        var r = await c.Login(new LoginDTO { Email = "prueba@alumno.buap.mx", Password = "Prueba123" });

        var obj = Assert.IsType<ObjectResult>(r.Result);
        Assert.Equal(403, obj.StatusCode);
    }

    // Puede FALLAR si no aplicaste el cambio de poner el correo en minúsculas dentro de Login.
    [Fact]
    public async Task Login_ignora_mayusculas_en_el_correo()
    {
        var (c, db, _) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario());
        db.SaveChanges();

        var r = await c.Login(new LoginDTO { Email = "PRUEBA@ALUMNO.BUAP.MX", Password = "Prueba123" });

        Assert.IsType<OkObjectResult>(r.Result);
    }

    // ---------- Verificar correo ----------

    [Fact]
    public async Task Verificar_con_el_codigo_correcto_activa_la_cuenta_y_da_token()
    {
        var (c, db, _) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456");

        var r = await c.VerificarCorreo(new VerificarCorreoDTO { Email = u.Email, Codigo = "123456" });

        var ok = Assert.IsType<OkObjectResult>(r.Result);
        Assert.False(string.IsNullOrWhiteSpace(((AuthResponseDTO)ok.Value!).Token));
        Assert.True(u.EmailVerificado);
        Assert.Null(u.CodigoVerificacionHash);
        Assert.Null(u.CodigoExpira);
    }

    [Fact]
    public async Task Verificar_con_codigo_incorrecto_cuenta_el_intento()
    {
        var (c, db, _) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456");

        var r = await c.VerificarCorreo(new VerificarCorreoDTO { Email = u.Email, Codigo = "000000" });

        var bad = Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Equal("Código incorrecto o vencido.", bad.Value);
        Assert.Equal(1, u.IntentosCodigo);
        Assert.False(u.EmailVerificado);
    }

    [Fact]
    public async Task Verificar_con_codigo_vencido_falla_aunque_sea_el_correcto()
    {
        var (c, db, _) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456", minutosParaVencer: -1);

        var r = await c.VerificarCorreo(new VerificarCorreoDTO { Email = u.Email, Codigo = "123456" });

        Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.False(u.EmailVerificado);
    }

    [Fact]
    public async Task Despues_de_5_intentos_ya_no_acepta_ni_el_codigo_correcto()
    {
        var (c, db, _) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456", intentos: 5);

        var r = await c.VerificarCorreo(new VerificarCorreoDTO { Email = u.Email, Codigo = "123456" });

        var bad = Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Contains("Demasiados", (string)bad.Value!);
        Assert.False(u.EmailVerificado);
    }

    [Fact]
    public async Task El_codigo_no_se_puede_usar_dos_veces()
    {
        var (c, db, _) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456");
        var datos = new VerificarCorreoDTO { Email = u.Email, Codigo = "123456" };

        await c.VerificarCorreo(datos);
        var segunda = await c.VerificarCorreo(datos);

        Assert.IsType<BadRequestObjectResult>(segunda.Result);
    }

    [Fact]
    public async Task Verificar_un_correo_inexistente_da_el_mismo_mensaje_generico()
    {
        var (c, _, _) = Crear();

        var r = await c.VerificarCorreo(new VerificarCorreoDTO { Email = "nadie@alumno.buap.mx", Codigo = "123456" });

        var bad = Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Equal("Código incorrecto o vencido.", bad.Value);
    }

    // ---------- Reenviar código ----------

    [Fact]
    public async Task Reenviar_manda_un_codigo_nuevo_y_reinicia_los_intentos()
    {
        var (c, db, correo) = Crear();
        var u = Ayudantes.CrearPendiente(db, codigo: "123456", intentos: 4, minutosDesdeUltimoEnvio: 2);

        var r = await c.ReenviarCodigo(new ReenviarCodigoDTO { Email = u.Email });

        Assert.IsType<OkObjectResult>(r);
        var envio = Assert.Single(correo.Enviados);
        Assert.Equal(0, u.IntentosCodigo);
        Assert.True(BCrypt.Net.BCrypt.Verify(envio.Codigo, u.CodigoVerificacionHash!));
        Assert.False(BCrypt.Net.BCrypt.Verify("123456", u.CodigoVerificacionHash!)); // el código viejo ya no sirve
    }

    [Fact]
    public async Task Reenviar_dos_veces_seguidas_es_rechazado()
    {
        var (c, db, correo) = Crear();
        var u = Ayudantes.CrearPendiente(db, minutosDesdeUltimoEnvio: 2);
        var datos = new ReenviarCodigoDTO { Email = u.Email };

        await c.ReenviarCodigo(datos);
        var segunda = await c.ReenviarCodigo(datos);

        Assert.IsType<BadRequestObjectResult>(segunda);
        Assert.Single(correo.Enviados);
    }

    [Fact]
    public async Task Reenviar_a_un_correo_desconocido_responde_igual_y_no_envia_nada()
    {
        var (c, _, correo) = Crear();

        var r = await c.ReenviarCodigo(new ReenviarCodigoDTO { Email = "nadie@alumno.buap.mx" });

        Assert.IsType<OkObjectResult>(r);
        Assert.Empty(correo.Enviados);
    }

    [Fact]
    public async Task Reenviar_a_una_cuenta_ya_verificada_no_envia_nada()
    {
        var (c, db, correo) = Crear();
        db.Usuarios.Add(Ayudantes.CrearUsuario(verificado: true));
        db.SaveChanges();

        var r = await c.ReenviarCodigo(new ReenviarCodigoDTO { Email = "prueba@alumno.buap.mx" });

        Assert.IsType<OkObjectResult>(r);
        Assert.Empty(correo.Enviados);
    }
}