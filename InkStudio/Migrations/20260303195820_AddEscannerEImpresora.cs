using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddEscannerEImpresora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsarEscanner",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsarImpresora",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "UsarEscanner", "UsarImpresora" },
                values: new object[] { false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsarEscanner",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "UsarImpresora",
                table: "Configuracion");
        }
    }
}
