using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmisorToCatTipoGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmisorId",
                table: "cat_tipo_gasto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_tipo_gasto_EmisorId",
                table: "cat_tipo_gasto",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_cat_tipo_gasto_TBL_Emisores_EmisorId",
                table: "cat_tipo_gasto",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cat_tipo_gasto_TBL_Emisores_EmisorId",
                table: "cat_tipo_gasto");

            migrationBuilder.DropIndex(
                name: "IX_cat_tipo_gasto_EmisorId",
                table: "cat_tipo_gasto");

            migrationBuilder.DropColumn(
                name: "EmisorId",
                table: "cat_tipo_gasto");
        }
    }
}
