using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupFieldsToConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BackupAutomaticoActivo",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BackupCopiarAutomaticamenteNube",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BackupFrecuencia",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BackupHora",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BackupMantenerUltimos",
                table: "Configuracion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BackupRutaNube",
                table: "Configuracion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackupServicioNube",
                table: "Configuracion",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BackupAutomaticoActivo", "BackupCopiarAutomaticamenteNube", "BackupFrecuencia", "BackupHora", "BackupMantenerUltimos", "BackupRutaNube", "BackupServicioNube" },
                values: new object[] { false, true, 0, 840, 10, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupAutomaticoActivo",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupCopiarAutomaticamenteNube",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupFrecuencia",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupHora",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupMantenerUltimos",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupRutaNube",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BackupServicioNube",
                table: "Configuracion");
        }
    }
}
