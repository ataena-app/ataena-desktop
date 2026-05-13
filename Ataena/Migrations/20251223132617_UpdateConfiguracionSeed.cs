using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfiguracionSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Direccion", "NombreEstudio" },
                values: new object[] { null, "Mi Estudio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Direccion", "NombreEstudio" },
                values: new object[] { null, "Ataena" });
        }
    }
}
