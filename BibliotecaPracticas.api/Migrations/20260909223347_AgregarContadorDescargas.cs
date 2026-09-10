using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaPracticas.api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarContadorDescargas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContadorDescargas",
                table: "Libros",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContadorDescargas",
                table: "Libros");
        }
    }
}
