using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterUniqueNitByActiveProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores",
                columns: new[] { "NIT", "EmisorId" },
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT_EmisorId",
                table: "proveedores",
                columns: new[] { "NIT", "EmisorId" },
                unique: true);
        }
    }
}
