using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMarcaUniqueIndexWithActivaFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas",
                columns: new[] { "Nombre", "EmisorId" },
                unique: true,
                filter: "\"Activa\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas",
                columns: new[] { "Nombre", "EmisorId" },
                unique: true);
        }
    }
}
