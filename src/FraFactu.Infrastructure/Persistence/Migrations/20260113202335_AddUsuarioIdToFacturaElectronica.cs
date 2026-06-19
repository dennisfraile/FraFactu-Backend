using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioIdToFacturaElectronica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CajaActualId",
                table: "Vendedores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_CajaActualId",
                table: "Vendedores",
                column: "CajaActualId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_UsuarioId",
                table: "Facturas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Usuarios_UsuarioId",
                table: "Facturas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Cajas_CajaActualId",
                table: "Vendedores",
                column: "CajaActualId",
                principalTable: "Cajas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Usuarios_UsuarioId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Cajas_CajaActualId",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_CajaActualId",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_UsuarioId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CajaActualId",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Facturas");
        }
    }
}
