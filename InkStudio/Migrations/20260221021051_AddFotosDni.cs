using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InkStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddFotosDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoDniPath",
                table: "Clientes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoDniTutorPath",
                table: "Clientes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoDniPath",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FotoDniTutorPath",
                table: "Clientes");
        }
    }
}
