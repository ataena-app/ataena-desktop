using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddTrabajoIdToCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trabajos_Citas_CitaId",
                table: "Trabajos");

            migrationBuilder.DropIndex(
                name: "IX_Trabajos_CitaId",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "CitaId",
                table: "Trabajos");

            migrationBuilder.AddColumn<int>(
                name: "TrabajoId",
                table: "Citas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citas_TrabajoId",
                table: "Citas",
                column: "TrabajoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Trabajos_TrabajoId",
                table: "Citas",
                column: "TrabajoId",
                principalTable: "Trabajos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Trabajos_TrabajoId",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_TrabajoId",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "TrabajoId",
                table: "Citas");

            migrationBuilder.AddColumn<int>(
                name: "CitaId",
                table: "Trabajos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajos_CitaId",
                table: "Trabajos",
                column: "CitaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajos_Citas_CitaId",
                table: "Trabajos",
                column: "CitaId",
                principalTable: "Citas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
