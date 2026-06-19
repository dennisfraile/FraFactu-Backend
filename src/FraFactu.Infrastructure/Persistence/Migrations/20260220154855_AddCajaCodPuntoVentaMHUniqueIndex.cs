using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCajaCodPuntoVentaMHUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVentaMH",
                table: "Cajas");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVentaMH",
                table: "Cajas",
                columns: new[] { "SucursalId", "CodPuntoVentaMH" },
                unique: true,
                filter: "\"CodPuntoVentaMH\" IS NOT NULL AND \"CodPuntoVentaMH\" != '' AND \"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVentaMH",
                table: "Cajas");
        }
    }
}
