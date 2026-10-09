using System.Security.Claims;
using BibliotecaPracticas.api.Controllers;
using BibliotecaPracticas.api.Data;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Models;
using BibliotecaPracticas.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaPracticas.api.Tests;

public class TextoUtilTests
{
    [Theory]
    [InlineData("Farmacología", "farmacologia")]
    [InlineData("  FARMACOLOGÍA  ", "farmacologia")]
    [InlineData("Trauma   y  Emergencias", "trauma y emergencias")]
    public void Clave_ignora_acentos_mayusculas_y_espacios(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoUtil.Clave(entrada));
    }

    [Fact]
    public void Limpiar_de_null_devuelve_cadena_vacia()
    {
        Assert.Equal(string.Empty, TextoUtil.Limpiar(null));
    }
}

public class CategoriasTests
{
    private static (CategoriasController c, AppDbContext db) Crear()
    {
        var db = Ayudantes.CrearContexto();
        return (new CategoriasController(db), db);
    }

    private static Categoria Sembrar(AppDbContext db, string nombre, bool activo = true)
    {
        var cat = new Categoria { Nombre = nombre, Activo = activo };
        db.Categorias.Add(cat);
        db.SaveChanges();
        return cat;
    }

    [Fact]
    public async Task Crea_una_categoria_nueva_y_limpia_los_espacios()
    {
        var (c, db) = Crear();

        var r = await c.PostCategoria(new CategoriaCreateDTO { Nombre = "  Pediatría   clínica " });

        var creada = Assert.IsType<CreatedAtActionResult>(r.Result);
        var dto = Assert.IsType<CategoriaDTO>(creada.Value);
        Assert.Equal("Pediatría clínica", dto.Nombre);
        Assert.True(dto.Activo);
        Assert.Single(db.Categorias);
    }

    [Theory]
    [InlineData("Farmacología")]
    [InlineData("farmacología")]
    [InlineData("FARMACOLOGIA")]
    [InlineData("  Farmacología  ")]
    [InlineData("Farmacologia")]
    public async Task Rechaza_duplicados_aunque_cambien_mayusculas_acentos_o_espacios(string nombre)
    {
        var (c, db) = Crear();
        Sembrar(db, "Farmacología");

        var r = await c.PostCategoria(new CategoriaCreateDTO { Nombre = nombre });

        Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Single(db.Categorias);
    }

    [Fact]
    public async Task Si_el_duplicado_esta_dado_de_baja_avisa_que_hay_que_reactivarlo()
    {
        var (c, db) = Crear();
        Sembrar(db, "Farmacología", activo: false);

        var r = await c.PostCategoria(new CategoriaCreateDTO { Nombre = "farmacologia" });

        var bad = Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Contains("dada de baja", (string)bad.Value!);
        Assert.Single(db.Categorias);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rechaza_nombres_vacios(string nombre)
    {
        var (c, db) = Crear();

        var r = await c.PostCategoria(new CategoriaCreateDTO { Nombre = nombre });

        Assert.IsType<BadRequestObjectResult>(r.Result);
        Assert.Empty(db.Categorias);
    }

    [Fact]
    public async Task Editar_con_el_nombre_de_otra_categoria_es_rechazado()
    {
        var (c, db) = Crear();
        Sembrar(db, "Farmacología");
        var pediatria = Sembrar(db, "Pediatría");

        var r = await c.PutCategoria(pediatria.Id, new CategoriaCreateDTO { Nombre = "FARMACOLOGÍA" });

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal("Pediatría", pediatria.Nombre);
    }

    [Fact]
    public async Task Editar_una_categoria_cambiando_solo_su_propia_capitalizacion_se_permite()
    {
        var (c, db) = Crear();
        var pediatria = Sembrar(db, "Pediatría");

        var r = await c.PutCategoria(pediatria.Id, new CategoriaCreateDTO { Nombre = "PEDIATRÍA" });

        Assert.IsType<NoContentResult>(r);
        Assert.Equal("PEDIATRÍA", pediatria.Nombre);
    }

    [Fact]
    public async Task Editar_una_categoria_inexistente_es_404()
    {
        var (c, _) = Crear();

        var r = await c.PutCategoria(999, new CategoriaCreateDTO { Nombre = "Lo que sea" });

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task Dar_de_baja_y_reactivar_cambia_el_estado()
    {
        var (c, db) = Crear();
        var cat = Sembrar(db, "Pediatría");

        await c.CambiarEstado(cat.Id, false);
        Assert.False(cat.Activo);

        await c.CambiarEstado(cat.Id, true);
        Assert.True(cat.Activo);
    }

    [Fact]
    public async Task El_catalogo_publico_no_muestra_las_dadas_de_baja()
    {
        var (c, db) = Crear();
        Sembrar(db, "Activa");
        Sembrar(db, "De baja", activo: false);

        var r = await c.GetCategorias();

        var ok = Assert.IsType<OkObjectResult>(r.Result);
        var lista = Assert.IsAssignableFrom<IEnumerable<CategoriaDTO>>(ok.Value);
        Assert.Equal("Activa", Assert.Single(lista).Nombre);
    }

    // Fallan si algún método de escritura solo tiene [Authorize] sin Roles = "Admin".
    [Theory]
    [InlineData(nameof(CategoriasController.PostCategoria))]
    [InlineData(nameof(CategoriasController.PutCategoria))]
    [InlineData(nameof(CategoriasController.CambiarEstado))]
    [InlineData(nameof(CategoriasController.GetTodasLasCategorias))]
    public void Gestionar_categorias_exige_rol_Admin(string metodo)
    {
        var atributo = typeof(CategoriasController).GetMethod(metodo)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal("Admin", atributo!.Roles);
    }
}

public class FavoritosTests
{
    private static FavoritosController Crear(AppDbContext db, int usuarioId = 1) =>
        new(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "Pruebas"))
                }
            }
        };

    private static Libro SembrarLibro(AppDbContext db, bool activo = true)
    {
        var libro = new Libro { Titulo = "Libro", RutaArchivo = "/uploads/libros/a.pdf", Activo = activo };
        db.Libros.Add(libro);
        db.SaveChanges();
        return libro;
    }

    [Fact]
    public async Task Agregar_un_libro_inexistente_es_404()
    {
        var db = Ayudantes.CrearContexto();

        var r = await Crear(db).AgregarFavorito(9999);

        Assert.IsType<NotFoundResult>(r);
        Assert.Empty(db.Favoritos);
    }

    [Fact]
    public async Task Agregar_un_libro_dado_de_baja_es_404()
    {
        var db = Ayudantes.CrearContexto();
        var libro = SembrarLibro(db, activo: false);

        var r = await Crear(db).AgregarFavorito(libro.Id);

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task Agregar_dos_veces_el_mismo_libro_no_duplica_el_favorito()
    {
        var db = Ayudantes.CrearContexto();
        var libro = SembrarLibro(db);
        var c = Crear(db);

        await c.AgregarFavorito(libro.Id);
        await c.AgregarFavorito(libro.Id);

        Assert.Single(db.Favoritos);
    }

    [Fact]
    public async Task Los_favoritos_son_por_usuario()
    {
        var db = Ayudantes.CrearContexto();
        var libro = SembrarLibro(db);

        await Crear(db, usuarioId: 1).AgregarFavorito(libro.Id);
        var r = await Crear(db, usuarioId: 2).GetFavoritos();

        var ok = Assert.IsType<OkObjectResult>(r.Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<int>>(ok.Value));
    }

    [Fact]
    public async Task Quitar_un_favorito_que_no_existe_es_404()
    {
        var db = Ayudantes.CrearContexto();

        var r = await Crear(db).QuitarFavorito(1);

        Assert.IsType<NotFoundResult>(r);
    }
}