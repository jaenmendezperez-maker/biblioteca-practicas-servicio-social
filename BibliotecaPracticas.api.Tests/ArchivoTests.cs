using System.Security.Claims;
using BibliotecaPracticas.api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace BibliotecaPracticas.api.Tests;

public class ArchivosTests : IDisposable
{
    private readonly string _raiz;
    private readonly EntornoFalso _env;

    public ArchivosTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "pruebas-" + Guid.NewGuid().ToString("N"));
        foreach (var carpeta in new[] { "libros", "portadas", "preview" })
            Directory.CreateDirectory(Path.Combine(_raiz, "wwwroot", "uploads", carpeta));

        File.WriteAllText(Path.Combine(_raiz, "wwwroot", "uploads", "portadas", "p.png"), "png");
        File.WriteAllText(Path.Combine(_raiz, "secreto.txt"), "no debe salir");
        CrearPdf("libro.pdf", 3);

        _env = new EntornoFalso(_raiz);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, recursive: true);
    }

    private void CrearPdf(string nombre, int paginas)
    {
        using var doc = new PdfDocument();
        for (int i = 0; i < paginas; i++) doc.AddPage();
        doc.Save(Path.Combine(_raiz, "wwwroot", "uploads", "libros", nombre));
    }

    private ArchivosController Crear(bool conSesion)
    {
        var contexto = new DefaultHttpContext();
        if (conSesion)
            contexto.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Alumno") }, "Pruebas"));

        return new ArchivosController(_env, NullLogger<ArchivosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = contexto }
        };
    }

    // ---------- Archivo completo ----------

    [Fact]
    public void El_pdf_completo_sin_sesion_es_401()
    {
        var r = Crear(conSesion: false).ObtenerArchivo("libros", "libro.pdf");

        Assert.IsType<UnauthorizedObjectResult>(r);
    }

    [Fact]
    public void El_pdf_completo_con_sesion_se_entrega()
    {
        var r = Crear(conSesion: true).ObtenerArchivo("libros", "libro.pdf");

        var archivo = Assert.IsType<PhysicalFileResult>(r);
        Assert.Equal("application/pdf", archivo.ContentType);
    }

    [Fact]
    public void Las_portadas_son_publicas()
    {
        var r = Crear(conSesion: false).ObtenerArchivo("portadas", "p.png");

        var archivo = Assert.IsType<PhysicalFileResult>(r);
        Assert.Equal("image/png", archivo.ContentType);
    }

    [Fact]
    public void Una_carpeta_no_permitida_es_400()
    {
        var r = Crear(conSesion: true).ObtenerArchivo("secretos", "x.pdf");

        Assert.IsType<BadRequestResult>(r);
    }

    [Fact]
    public void Un_archivo_inexistente_es_404()
    {
        var r = Crear(conSesion: true).ObtenerArchivo("libros", "no-existe.pdf");

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public void No_permite_salirse_de_la_carpeta_con_rutas_relativas()
    {
        var r = Crear(conSesion: true).ObtenerArchivo("portadas", "../../../secreto.txt");

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public void Descargar_agrega_la_cabecera_attachment()
    {
        var c = Crear(conSesion: true);

        c.ObtenerArchivo("libros", "libro.pdf", descargar: true);

        Assert.Contains("attachment", c.Response.Headers["Content-Disposition"].ToString());
    }

    // ---------- Lectura parcial ----------

    [Fact]
    public void El_fragmento_es_publico_y_no_pide_sesion()
    {
        CrearPdf("veinte.pdf", 20);

        var r = Crear(conSesion: false).ObtenerFragmento("veinte.pdf");

        Assert.IsType<PhysicalFileResult>(r);
    }

    [Theory]
    [InlineData(20, 5)]    // 25 % = 5
    [InlineData(100, 25)]  // 25 % = 25
    [InlineData(10, 5)]    // el mínimo son 5 páginas
    [InlineData(3, 2)]     // nunca entrega el libro completo: siempre deja una página fuera
    public void El_fragmento_trae_el_numero_correcto_de_paginas(int total, int esperadas)
    {
        CrearPdf("x.pdf", total);
        var c = Crear(conSesion: false);

        var r = c.ObtenerFragmento("x.pdf");

        var archivo = Assert.IsType<PhysicalFileResult>(r);
        Assert.Equal(total.ToString(), c.Response.Headers["X-Total-Paginas"].ToString());
        Assert.Equal(esperadas.ToString(), c.Response.Headers["X-Paginas-Fragmento"].ToString());

        using var fragmento = PdfReader.Open(archivo.FileName, PdfDocumentOpenMode.Import);
        Assert.Equal(esperadas, fragmento.PageCount);
    }

    [Fact]
    public void El_fragmento_de_algo_que_no_es_pdf_es_400()
    {
        var r = Crear(conSesion: false).ObtenerFragmento("secreto.txt");

        Assert.IsType<BadRequestResult>(r);
    }

    [Fact]
    public void El_fragmento_de_un_libro_inexistente_es_404()
    {
        var r = Crear(conSesion: false).ObtenerFragmento("no-existe.pdf");

        Assert.IsType<NotFoundResult>(r);
    }
}