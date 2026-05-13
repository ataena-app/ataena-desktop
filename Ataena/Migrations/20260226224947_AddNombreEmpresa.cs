using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddNombreEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreEmpresa",
                table: "Configuracion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                column: "NombreEmpresa",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreEmpresa",
                table: "Configuracion");
        }
    }
}
