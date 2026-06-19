using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowReuseDeactivatedVendedorCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);
        }
    }
}
