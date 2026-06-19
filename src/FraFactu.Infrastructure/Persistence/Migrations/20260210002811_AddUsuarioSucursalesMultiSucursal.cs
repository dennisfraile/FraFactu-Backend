using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioSucursalesMultiSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregar nueva columna AccesoTodasSucursales
            migrationBuilder.AddColumn<bool>(
                name: "AccesoTodasSucursales",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 2. Crear tabla join UsuariosSucursales
            migrationBuilder.CreateTable(
                name: "UsuariosSucursales",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSucursales", x => new { x.UsuarioId, x.SucursalId });
                    table.ForeignKey(
                        name: "FK_UsuariosSucursales_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UsuariosSucursales_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioSucursal_SucursalId",
                table: "UsuariosSucursales",
                column: "SucursalId");

            // 3. Migrar datos: usuarios con SucursalId → UsuariosSucursales
            migrationBuilder.Sql(@"
                INSERT INTO ""UsuariosSucursales"" (""UsuarioId"", ""SucursalId"")
                SELECT ""Id"", ""SucursalId"" FROM ""Usuarios""
                WHERE ""SucursalId"" IS NOT NULL
            ");

            // 4. Usuarios sin SucursalId (admins) → AccesoTodasSucursales = true
            migrationBuilder.Sql(@"
                UPDATE ""Usuarios"" SET ""AccesoTodasSucursales"" = true
                WHERE ""SucursalId"" IS NULL
            ");

            // 5. Ahora sí eliminar la columna SucursalId
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_TBL_Sucursales_SucursalId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_SucursalId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Usuarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuariosSucursales");

            migrationBuilder.DropColumn(
                name: "AccesoTodasSucursales",
                table: "Usuarios");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_SucursalId",
                table: "Usuarios",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_TBL_Sucursales_SucursalId",
                table: "Usuarios",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id");
        }
    }
}
