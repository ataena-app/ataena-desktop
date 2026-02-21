using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddDiasFestivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiasFestivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreIngles = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    EsPersonalizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CodigoSubdivision = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    EsFijo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColorFondo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasFestivos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasFestivos_Anio_Fecha",
                table: "DiasFestivos",
                columns: new[] { "Anio", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_DiasFestivos_Fecha_Nombre",
                table: "DiasFestivos",
                columns: new[] { "Fecha", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiasFestivos");
        }
    }
}
