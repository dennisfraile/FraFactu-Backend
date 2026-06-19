using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableMultiTenantCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * Conflict Resolution: Attributes in this migration already exist in the database.
             * Commenting out to mark as applied without re-executing.
             */
            /*
           migrationBuilder.DropIndex(
               name: "IX_Vendedores_EmisorId",
               table: "Vendedores");

           migrationBuilder.DropIndex(
               name: "IX_Cajas_SucursalId",
               table: "Cajas");

           migrationBuilder.DropIndex(
               name: "IX_Bodegas_Codigo",
               table: "bodegas");

           migrationBuilder.AlterColumn<decimal>(
               name: "PrecioVenta",
               table: "TBL_ProductosServicios",
               type: "numeric(18,8)",
               precision: 18,
               scale: 8,
               nullable: false,
               oldClrType: typeof(decimal),
               oldType: "numeric(18,2)",
               oldPrecision: 18,
               oldScale: 2);

           migrationBuilder.CreateIndex(
               name: "IX_Vendedores_Emisor_Codigo",
               table: "Vendedores",
               columns: new[] { "EmisorId", "Codigo" },
               unique: true);

           migrationBuilder.CreateIndex(
               name: "IX_Facturas_CatCondicionOperacionId",
               table: "Facturas",
               column: "CatCondicionOperacionId");

           migrationBuilder.CreateIndex(
               name: "IX_FacturaPagos_CatPlazoId",
               table: "FacturaPagos",
               column: "CatPlazoId");

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

           migrationBuilder.AddForeignKey(
               name: "FK_FacturaPagos_cat_plazo_CatPlazoId",
               table: "FacturaPagos",
               column: "CatPlazoId",
               principalTable: "cat_plazo",
               principalColumn: "Id");

           migrationBuilder.AddForeignKey(
               name: "FK_Facturas_cat_condicion_operacion_CatCondicionOperacionId",
               table: "Facturas",
               column: "CatCondicionOperacionId",
               principalTable: "cat_condicion_operacion",
               principalColumn: "Id",
               onDelete: ReferentialAction.Cascade);
           */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaPagos_cat_plazo_CatPlazoId",
                table: "FacturaPagos");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_cat_condicion_operacion_CatCondicionOperacionId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CatCondicionOperacionId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_FacturaPagos_CatPlazoId",
                table: "FacturaPagos");

            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_Codigo",
                table: "Cajas");

            migrationBuilder.DropIndex(
                name: "IX_Bodegas_Sucursal_Codigo",
                table: "bodegas");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioVenta",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_EmisorId",
                table: "Vendedores",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_SucursalId",
                table: "Cajas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Codigo",
                table: "bodegas",
                column: "Codigo",
                unique: true);
        }
    }
}
