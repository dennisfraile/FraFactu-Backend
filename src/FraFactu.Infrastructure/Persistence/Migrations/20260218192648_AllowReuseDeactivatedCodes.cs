using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowReuseDeactivatedCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop indexes safely (some may not exist in DB)
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Sucursal_Emisor_Codigo\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Cajas_Sucursal_Codigo\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Bodegas_Sucursal_Codigo\";");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_Emisor_Codigo",
                table: "TBL_Sucursales",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_Codigo",
                table: "Cajas",
                columns: new[] { "SucursalId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Sucursal_Codigo",
                table: "bodegas",
                columns: new[] { "SucursalId", "Codigo" },
                unique: true,
                filter: "\"Activa\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sucursal_Emisor_Codigo",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_Codigo",
                table: "Cajas");

            migrationBuilder.DropIndex(
                name: "IX_Bodegas_Sucursal_Codigo",
                table: "bodegas");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_Emisor_Codigo",
                table: "TBL_Sucursales",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_Codigo",
                table: "Cajas",
                columns: new[] { "SucursalId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Sucursal_Codigo",
                table: "bodegas",
                columns: new[] { "SucursalId", "Codigo" },
                unique: true);
        }
    }
}
