using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmisorToMarcaAndCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmisorId",
                table: "marcas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmisorId",
                table: "categorias",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_marcas_EmisorId",
                table: "marcas",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_EmisorId",
                table: "categorias",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_categorias_TBL_Emisores_EmisorId",
                table: "categorias",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_marcas_TBL_Emisores_EmisorId",
                table: "marcas",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categorias_TBL_Emisores_EmisorId",
                table: "categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_marcas_TBL_Emisores_EmisorId",
                table: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_marcas_EmisorId",
                table: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_categorias_EmisorId",
                table: "categorias");

            migrationBuilder.DropColumn(
                name: "EmisorId",
                table: "marcas");

            migrationBuilder.DropColumn(
                name: "EmisorId",
                table: "categorias");
        }
    }
}
