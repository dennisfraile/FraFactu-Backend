using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmisorIdToProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmisorId",
                table: "proveedores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_proveedores_EmisorId",
                table: "proveedores",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_proveedores_TBL_Emisores_EmisorId",
                table: "proveedores",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_proveedores_TBL_Emisores_EmisorId",
                table: "proveedores");

            migrationBuilder.DropIndex(
                name: "IX_proveedores_EmisorId",
                table: "proveedores");

            migrationBuilder.DropColumn(
                name: "EmisorId",
                table: "proveedores");
        }
    }
}
