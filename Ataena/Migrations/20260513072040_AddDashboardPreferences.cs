using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DashboardMostrarAccionesRapidas",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DashboardMostrarAlertas",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DashboardMostrarEconomia",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DashboardMostrarEstadisticas",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DashboardMostrarAccionesRapidas", "DashboardMostrarAlertas", "DashboardMostrarEconomia", "DashboardMostrarEstadisticas" },
                values: new object[] { true, true, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DashboardMostrarAccionesRapidas",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "DashboardMostrarAlertas",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "DashboardMostrarEconomia",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "DashboardMostrarEstadisticas",
                table: "Configuracion");
        }
    }
}
