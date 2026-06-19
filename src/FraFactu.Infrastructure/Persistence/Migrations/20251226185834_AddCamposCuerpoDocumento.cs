using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposCuerpoDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodTributo",
                table: "factura_detalles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoRelacionado",
                table: "factura_detalles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioSugeridoVenta",
                table: "factura_detalles",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodTributo",
                table: "factura_detalles");

            migrationBuilder.DropColumn(
                name: "NumeroDocumentoRelacionado",
                table: "factura_detalles");

            migrationBuilder.DropColumn(
                name: "PrecioSugeridoVenta",
                table: "factura_detalles");
        }
    }
}
