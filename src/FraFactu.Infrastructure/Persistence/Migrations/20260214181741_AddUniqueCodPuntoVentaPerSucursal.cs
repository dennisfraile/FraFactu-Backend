using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCodPuntoVentaPerSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVenta",
                table: "Cajas",
                columns: new[] { "SucursalId", "CodPuntoVenta" },
                unique: true,
                filter: "\"CodPuntoVenta\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVenta",
                table: "Cajas");
        }
    }
}
