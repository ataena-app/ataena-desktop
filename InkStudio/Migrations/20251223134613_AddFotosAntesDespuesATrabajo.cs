using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddFotosAntesDespuesATrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoAntesPath",
                table: "Trabajos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoDespuesPath",
                table: "Trabajos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoAntesPath",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "FotoDespuesPath",
                table: "Trabajos");
        }
    }
}
