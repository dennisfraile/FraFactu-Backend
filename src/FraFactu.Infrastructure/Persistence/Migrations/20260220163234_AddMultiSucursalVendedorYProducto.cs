using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSucursalVendedorYProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // PASO 1: Agregar columnas AccesoTodasSucursales
            // ============================================================
            migrationBuilder.AddColumn<bool>(
                name: "AccesoTodasSucursales",
                table: "Vendedores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoTodasSucursales",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // ============================================================
            // PASO 2: Crear tablas junction
            // ============================================================
            migrationBuilder.CreateTable(
                name: "VendedoresSucursales",
                columns: table => new
                {
                    VendedorId = table.Column<int>(type: "integer", nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendedoresSucursales", x => new { x.VendedorId, x.SucursalId });
                    table.ForeignKey(
                        name: "FK_VendedoresSucursales_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendedoresSucursales_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductosServiciosSucursales",
                columns: table => new
                {
                    ProductoServicioId = table.Column<int>(type: "integer", nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosServiciosSucursales", x => new { x.ProductoServicioId, x.SucursalId });
                    table.ForeignKey(
                        name: "FK_ProductosServiciosSucursales_TBL_ProductosServicios_Product~",
                        column: x => x.ProductoServicioId,
                        principalTable: "TBL_ProductosServicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductosServiciosSucursales_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendedorSucursal_SucursalId",
                table: "VendedoresSucursales",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoServicioSucursal_SucursalId",
                table: "ProductosServiciosSucursales",
                column: "SucursalId");

            // ============================================================
            // PASO 3: Migrar datos existentes ANTES de eliminar columnas
            // ============================================================

            // Vendedores: copiar SucursalId existente a junction table
            migrationBuilder.Sql(
                @"INSERT INTO ""VendedoresSucursales"" (""VendedorId"", ""SucursalId"")
                  SELECT ""Id"", ""SucursalId"" FROM ""Vendedores""
                  WHERE ""SucursalId"" IS NOT NULL");

            // Vendedores sin sucursal => AccesoTodasSucursales = true
            migrationBuilder.Sql(
                @"UPDATE ""Vendedores"" SET ""AccesoTodasSucursales"" = true
                  WHERE ""SucursalId"" IS NULL");

            // ProductosServicios: copiar SucursalId existente a junction table
            migrationBuilder.Sql(
                @"INSERT INTO ""ProductosServiciosSucursales"" (""ProductoServicioId"", ""SucursalId"")
                  SELECT ""Id"", ""SucursalId"" FROM ""TBL_ProductosServicios""
                  WHERE ""SucursalId"" IS NOT NULL");

            // ProductosServicios sin sucursal => AccesoTodasSucursales = true
            migrationBuilder.Sql(
                @"UPDATE ""TBL_ProductosServicios"" SET ""AccesoTodasSucursales"" = true
                  WHERE ""SucursalId"" IS NULL");

            // ============================================================
            // PASO 4: Eliminar FKs, índices y columnas antiguas
            // ============================================================
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_TBL_Sucursales_SucursalId",
                table: "Vendedores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_TBL_Sucursales_SucursalId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_SucursalId",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Sucursal",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "TBL_ProductosServicios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductosServiciosSucursales");

            migrationBuilder.DropTable(
                name: "VendedoresSucursales");

            migrationBuilder.DropColumn(
                name: "AccesoTodasSucursales",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "AccesoTodasSucursales",
                table: "TBL_ProductosServicios");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Vendedores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_SucursalId",
                table: "Vendedores",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Sucursal",
                table: "TBL_ProductosServicios",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ProductosServicios_TBL_Sucursales_SucursalId",
                table: "TBL_ProductosServicios",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_TBL_Sucursales_SucursalId",
                table: "Vendedores",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
