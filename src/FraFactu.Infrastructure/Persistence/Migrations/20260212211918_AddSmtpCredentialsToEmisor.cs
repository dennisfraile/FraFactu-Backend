using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpCredentialsToEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailHabilitado",
                table: "TBL_Emisores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailRemitente",
                table: "TBL_Emisores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "TBL_Emisores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "TBL_Emisores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "TBL_Emisores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUser",
                table: "TBL_Emisores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailHabilitado",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "EmailRemitente",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "SmtpUser",
                table: "TBL_Emisores");
        }
    }
}
