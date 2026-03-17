using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ataena.Migrations
{
    /// <inheritdoc />
    public partial class AddMenoresYRenovacionConsentimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsentimientoRenovacionId",
                table: "Consentimientos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DniTutorFirmante",
                table: "Consentimientos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EdadClienteAlFirmar",
                table: "Consentimientos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsConsentimientoMenor",
                table: "Consentimientos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirmaMenorBase64",
                table: "Consentimientos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmaTutorBase64",
                table: "Consentimientos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreTutorFirmante",
                table: "Consentimientos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Renovado",
                table: "Consentimientos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApellidosTutor",
                table: "Clientes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DniTutor",
                table: "Clientes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreTutor",
                table: "Clientes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoTutor",
                table: "Clientes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentimientoRenovacionId",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "DniTutorFirmante",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "EdadClienteAlFirmar",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "EsConsentimientoMenor",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "FirmaMenorBase64",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "FirmaTutorBase64",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "NombreTutorFirmante",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "Renovado",
                table: "Consentimientos");

            migrationBuilder.DropColumn(
                name: "ApellidosTutor",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DniTutor",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NombreTutor",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TelefonoTutor",
                table: "Clientes");
        }
    }
}
