using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BibliotecaPracticas.api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSemestres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Semestres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semestres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibroSemestre",
                columns: table => new
                {
                    LibrosId = table.Column<int>(type: "integer", nullable: false),
                    SemestresId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibroSemestre", x => new { x.LibrosId, x.SemestresId });
                    table.ForeignKey(
                        name: "FK_LibroSemestre_Libros_LibrosId",
                        column: x => x.LibrosId,
                        principalTable: "Libros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibroSemestre_Semestres_SemestresId",
                        column: x => x.SemestresId,
                        principalTable: "Semestres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Semestres",
                columns: new[] { "Id", "Nombre", "Numero" },
                values: new object[,]
                {
                    { 1, "Semestre 1", 1 },
                    { 2, "Semestre 2", 2 },
                    { 3, "Semestre 3", 3 },
                    { 4, "Semestre 4", 4 },
                    { 5, "Semestre 5", 5 },
                    { 6, "Semestre 6", 6 },
                    { 7, "Semestre 7", 7 },
                    { 8, "Semestre 8", 8 },
                    { 9, "Semestre 9", 9 },
                    { 10, "Semestre 10", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibroSemestre_SemestresId",
                table: "LibroSemestre",
                column: "SemestresId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibroSemestre");

            migrationBuilder.DropTable(
                name: "Semestres");
        }
    }
}
