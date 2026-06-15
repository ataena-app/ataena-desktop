using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilNormativoDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PerfilNormativoDni",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                column: "PerfilNormativoDni",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerfilNormativoDni",
                table: "Configuracion");
        }
    }
}
