using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaPracticas.api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Libros",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Categorias",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Libros");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Categorias");
        }
    }
}
