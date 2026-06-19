using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTipoGastoUniqueIndexPerEmisorActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_cat_tipo_gasto_EmisorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CatTipoGasto_Codigo"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CatTipoGasto_Emisor_Codigo"";");

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Emisor_Codigo",
                table: "cat_tipo_gasto",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CatTipoGasto_Emisor_Codigo",
                table: "cat_tipo_gasto");

            migrationBuilder.CreateIndex(
                name: "IX_cat_tipo_gasto_EmisorId",
                table: "cat_tipo_gasto",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Codigo",
                table: "cat_tipo_gasto",
                column: "Codigo",
                unique: true);
        }
    }
}
