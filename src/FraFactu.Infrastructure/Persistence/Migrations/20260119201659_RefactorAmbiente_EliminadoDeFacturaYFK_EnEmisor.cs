using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAmbiente_EliminadoDeFacturaYFK_EnEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ambiente",
                table: "Facturas");

            migrationBuilder.RenameColumn(
                name: "Ambiente",
                table: "TBL_Emisores",
                newName: "CatAmbienteDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_CatAmbienteDestinoId",
                table: "TBL_Emisores",
                column: "CatAmbienteDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Emisores_cat_ambiente_destino_CatAmbienteDestinoId",
                table: "TBL_Emisores",
                column: "CatAmbienteDestinoId",
                principalTable: "cat_ambiente_destino",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Emisores_cat_ambiente_destino_CatAmbienteDestinoId",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_CatAmbienteDestinoId",
                table: "TBL_Emisores");

            migrationBuilder.RenameColumn(
                name: "CatAmbienteDestinoId",
                table: "TBL_Emisores",
                newName: "Ambiente");

            migrationBuilder.AddColumn<string>(
                name: "Ambiente",
                table: "Facturas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
