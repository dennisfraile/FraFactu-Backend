using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursalIdToProductoServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_externas_proveedores_ProveedorId1",
                table: "compras_externas");

            migrationBuilder.DropIndex(
                name: "IX_compras_externas_ProveedorId1",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "ProveedorId1",
                table: "compras_externas");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_TBL_Sucursales_SucursalId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Sucursal",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "TBL_ProductosServicios");

            migrationBuilder.AddColumn<int>(
                name: "ProveedorId1",
                table: "compras_externas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_externas_ProveedorId1",
                table: "compras_externas",
                column: "ProveedorId1");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_externas_proveedores_ProveedorId1",
                table: "compras_externas",
                column: "ProveedorId1",
                principalTable: "proveedores",
                principalColumn: "Id");
        }
    }
}
