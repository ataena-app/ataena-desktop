using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddDuracionesYEstadoATrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionEstimadaMinutos",
                table: "Trabajos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuracionRealMinutos",
                table: "Trabajos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Trabajos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracionEstimadaMinutos",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "DuracionRealMinutos",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Trabajos");
        }
    }
}
