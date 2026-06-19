using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmisorCodigos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodEstable",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodEstableMH",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodPuntoVenta",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodPuntoVentaMH",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodEstable",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CodEstableMH",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CodPuntoVenta",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CodPuntoVentaMH",
                table: "TBL_Emisores");
        }
    }
}
