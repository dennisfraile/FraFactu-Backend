using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixReceptorTipoDocumentoFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_tipo_doc_CatTipoDocumentoId",
                table: "Receptores");

            migrationBuilder.RenameColumn(
                name: "CatTipoDocumentoId",
                table: "Receptores",
                newName: "CatTipoDocumentoIdentificacionReceptorId");

            migrationBuilder.RenameIndex(
                name: "IX_Receptores_CatTipoDocumentoId",
                table: "Receptores",
                newName: "IX_Receptores_CatTipoDocumentoIdentificacionReceptorId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CatTipoContingenciaId",
                table: "Facturas",
                column: "CatTipoContingenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_cat_tipo_contingencia_CatTipoContingenciaId",
                table: "Facturas",
                column: "CatTipoContingenciaId",
                principalTable: "cat_tipo_contingencia",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_tipo_documento_identificacion_receptor_CatTi~",
                table: "Receptores",
                column: "CatTipoDocumentoIdentificacionReceptorId",
                principalTable: "cat_tipo_documento_identificacion_receptor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_cat_tipo_contingencia_CatTipoContingenciaId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_tipo_documento_identificacion_receptor_CatTi~",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CatTipoContingenciaId",
                table: "Facturas");

            migrationBuilder.RenameColumn(
                name: "CatTipoDocumentoIdentificacionReceptorId",
                table: "Receptores",
                newName: "CatTipoDocumentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Receptores_CatTipoDocumentoIdentificacionReceptorId",
                table: "Receptores",
                newName: "IX_Receptores_CatTipoDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_tipo_doc_CatTipoDocumentoId",
                table: "Receptores",
                column: "CatTipoDocumentoId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
