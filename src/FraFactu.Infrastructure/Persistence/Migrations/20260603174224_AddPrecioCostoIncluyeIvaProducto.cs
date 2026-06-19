using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecioCostoIncluyeIvaProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CostoIncluyeIva",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrecioIncluyeIva",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoIncluyeIva",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PrecioIncluyeIva",
                table: "TBL_ProductosServicios");
        }
    }
}
