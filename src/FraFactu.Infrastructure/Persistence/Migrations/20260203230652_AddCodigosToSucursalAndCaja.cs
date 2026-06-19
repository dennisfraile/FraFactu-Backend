using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigosToSucursalAndCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "CodPuntoVenta",
                table: "Cajas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CodPuntoVentaMH",
                table: "Cajas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodEstable",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodEstableMH",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodPuntoVenta",
                table: "Cajas");

            migrationBuilder.DropColumn(
                name: "CodPuntoVentaMH",
                table: "Cajas");
        }
    }
}
