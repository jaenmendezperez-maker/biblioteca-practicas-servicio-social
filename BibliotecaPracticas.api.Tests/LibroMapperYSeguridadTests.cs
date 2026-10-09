using BibliotecaPracticas.api.Controllers;
using BibliotecaPracticas.api.DTOs;
using BibliotecaPracticas.api.Models;
using Microsoft.AspNetCore.Authorization;

namespace BibliotecaPracticas.api.Tests;

public class LibroMapperTests
{
    [Fact]
    public void Ordena_los_semestres_por_numero()
    {
        var libro = new Libro
        {
            Titulo = "Prueba",
            RutaArchivo = "/uploads/libros/a.pdf",
            Semestres =
            {
                new Semestre { Id = 3, Numero = 3, Nombre = "Semestre 3" },
                new Semestre { Id = 1, Numero = 1, Nombre = "Semestre 1" }
            }
        };

        var dto = LibroMapper.ToDto(libro);

        Assert.Equal(new[] { 1, 3 }, dto.Semestres.Select(s => s.Numero));
    }

    [Fact]
    public void Convierte_el_json_de_paginas_y_de_tabla_de_contenido()
    {
        var libro = new Libro
        {
            Titulo = "Prueba",
            RutaArchivo = "/uploads/libros/a.pdf",
            PaginasPreview = "[\"/uploads/preview/a.png\"]",
            TablaContenido = "[{\"titulo\":\"Introducción\",\"pagina\":4}]"
        };

        var dto = LibroMapper.ToDto(libro);

        Assert.Equal("/uploads/preview/a.png", Assert.Single(dto.PaginasPreview!));
        var item = Assert.Single(dto.TablaContenido!);
        Assert.Equal("Introducción", item.Titulo);
        Assert.Equal(4, item.Pagina);
    }

    [Fact]
    public void Un_libro_sin_categoria_ni_semestres_no_truena()
    {
        var libro = new Libro { Titulo = "Prueba", RutaArchivo = "/uploads/libros/a.pdf" };

        var dto = LibroMapper.ToDto(libro);

        Assert.Null(dto.CategoriaNombre);
        Assert.Empty(dto.Semestres);
        Assert.Null(dto.PaginasPreview);
    }
}

public class SeguridadDeRolesTests
{
    // Fallan si algún método sensible solo tiene [Authorize] sin Roles = "Admin".
    // Con el registro abierto a alumnos, eso les permitiría subir libros o cambiarse el rol.
    [Theory]
    [InlineData(typeof(UsuariosController), nameof(UsuariosController.GetUsuarios))]
    [InlineData(typeof(UsuariosController), nameof(UsuariosController.GetUsuario))]
    [InlineData(typeof(UsuariosController), nameof(UsuariosController.CambiarRol))]
    [InlineData(typeof(UsuariosController), nameof(UsuariosController.CambiarEstado))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.PostLibro))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.PutLibro))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.CambiarEstado))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.ReemplazarPdf))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.ActualizarMetadatosExtraidos))]
    [InlineData(typeof(LibrosController), nameof(LibrosController.SubirImagenExtraida))]
    public void Los_metodos_sensibles_exigen_rol_Admin(Type controlador, string metodo)
    {
        var atributo = controlador.GetMethod(metodo)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal("Admin", atributo!.Roles);
    }
}