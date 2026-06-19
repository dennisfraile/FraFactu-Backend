using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateTreasuryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodEstable",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodEstableMH",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodEstable",
                table: "TBL_Sucursales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodEstableMH",
                table: "TBL_Sucursales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

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
    }
}
