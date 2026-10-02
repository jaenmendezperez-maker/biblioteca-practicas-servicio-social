using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaPracticas.api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPaginasYTamano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroPaginas",
                table: "Favoritos");

            migrationBuilder.DropColumn(
                name: "TamanoBytes",
                table: "Favoritos");

            migrationBuilder.AddColumn<int>(
                name: "NumeroPaginas",
                table: "Libros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TamanoBytes",
                table: "Libros",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroPaginas",
                table: "Libros");

            migrationBuilder.DropColumn(
                name: "TamanoBytes",
                table: "Libros");

            migrationBuilder.AddColumn<int>(
                name: "NumeroPaginas",
                table: "Favoritos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TamanoBytes",
                table: "Favoritos",
                type: "bigint",
                nullable: true);
        }
    }
}
