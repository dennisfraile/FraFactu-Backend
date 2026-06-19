using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueIndexesPerEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT",
                table: "proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Marcas_Nombre",
                table: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Codigo",
                table: "categorias");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores",
                columns: new[] { "NIT", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas",
                columns: new[] { "Nombre", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Codigo_EmisorId",
                table: "categorias",
                columns: new[] { "Codigo", "EmisorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Codigo_EmisorId",
                table: "categorias");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT",
                table: "proveedores",
                column: "NIT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre",
                table: "marcas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Codigo",
                table: "categorias",
                column: "Codigo",
                unique: true);
        }
    }
}
