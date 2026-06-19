using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioCajasRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Cajas_CajaActualId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CajaActualId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CajaActualId",
                table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "UsuariosCajas",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CajaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosCajas", x => new { x.UsuarioId, x.CajaId });
                    table.ForeignKey(
                        name: "FK_UsuariosCajas_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuariosCajas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosCajas_CajaId",
                table: "UsuariosCajas",
                column: "CajaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosCajas");

            migrationBuilder.AddColumn<int>(
                name: "CajaActualId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CajaActualId",
                table: "Usuarios",
                column: "CajaActualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Cajas_CajaActualId",
                table: "Usuarios",
                column: "CajaActualId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
