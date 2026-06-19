using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComisionAndSucursalToVendedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeComision",
                table: "Vendedores",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Vendedores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_SucursalId",
                table: "Vendedores",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_TBL_Sucursales_SucursalId",
                table: "Vendedores",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_TBL_Sucursales_SucursalId",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_SucursalId",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "PorcentajeComision",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Vendedores");
        }
    }
}
