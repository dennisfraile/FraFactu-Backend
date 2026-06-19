using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursalIdToCompraExterna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProveedorId1",
                table: "compras_externas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "compras_externas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_compras_externas_ProveedorId1",
                table: "compras_externas",
                column: "ProveedorId1");

            migrationBuilder.CreateIndex(
                name: "IX_compras_externas_SucursalId",
                table: "compras_externas",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_compras_externas_TBL_Sucursales_SucursalId",
                table: "compras_externas",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_compras_externas_proveedores_ProveedorId1",
                table: "compras_externas",
                column: "ProveedorId1",
                principalTable: "proveedores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compras_externas_TBL_Sucursales_SucursalId",
                table: "compras_externas");

            migrationBuilder.DropForeignKey(
                name: "FK_compras_externas_proveedores_ProveedorId1",
                table: "compras_externas");

            migrationBuilder.DropIndex(
                name: "IX_compras_externas_ProveedorId1",
                table: "compras_externas");

            migrationBuilder.DropIndex(
                name: "IX_compras_externas_SucursalId",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "ProveedorId1",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "compras_externas");
        }
    }
}
