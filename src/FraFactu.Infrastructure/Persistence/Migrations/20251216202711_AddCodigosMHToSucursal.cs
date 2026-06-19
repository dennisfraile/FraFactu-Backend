using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigosMHToSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoEstablecimiento",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEstablecimiento",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales");
        }
    }
}
