using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMhCredentialsToEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ambiente",
                table: "TBL_Emisores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MhClaveApi",
                table: "TBL_Emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MhLlavePrivada",
                table: "TBL_Emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MhLlavePublica",
                table: "TBL_Emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MhPassPrivada",
                table: "TBL_Emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MhUsuario",
                table: "TBL_Emisores",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ambiente",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhClaveApi",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhLlavePrivada",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhLlavePublica",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhPassPrivada",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhUsuario",
                table: "TBL_Emisores");
        }
    }
}
