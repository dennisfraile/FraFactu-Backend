using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGmailOAuth2ToEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GmailConectado",
                table: "TBL_Emisores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GmailEmail",
                table: "TBL_Emisores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmailRefreshToken",
                table: "TBL_Emisores",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GmailConectado",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "GmailEmail",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "GmailRefreshToken",
                table: "TBL_Emisores");
        }
    }
}
