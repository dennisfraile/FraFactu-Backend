using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioToMovimientoInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "movimientos_inventario",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_UsuarioId",
                table: "movimientos_inventario",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_inventario_Usuarios_UsuarioId",
                table: "movimientos_inventario",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_inventario_Usuarios_UsuarioId",
                table: "movimientos_inventario");

            migrationBuilder.DropIndex(
                name: "IX_movimientos_inventario_UsuarioId",
                table: "movimientos_inventario");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "movimientos_inventario");
        }
    }
}
