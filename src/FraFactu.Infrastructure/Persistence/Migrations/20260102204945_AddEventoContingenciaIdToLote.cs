using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoContingenciaIdToLote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_doc_CatTipoDocResponsableId",
                table: "eventos_contingencia");

            migrationBuilder.AddColumn<int>(
                name: "EventoContingenciaId",
                table: "lotes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lotes_EventoContingenciaId",
                table: "lotes",
                column: "EventoContingenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_documento_identificacion_rece~",
                table: "eventos_contingencia",
                column: "CatTipoDocResponsableId",
                principalTable: "cat_tipo_documento_identificacion_receptor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_lotes_eventos_contingencia_EventoContingenciaId",
                table: "lotes",
                column: "EventoContingenciaId",
                principalTable: "eventos_contingencia",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_documento_identificacion_rece~",
                table: "eventos_contingencia");

            migrationBuilder.DropForeignKey(
                name: "FK_lotes_eventos_contingencia_EventoContingenciaId",
                table: "lotes");

            migrationBuilder.DropIndex(
                name: "IX_lotes_EventoContingenciaId",
                table: "lotes");

            migrationBuilder.DropColumn(
                name: "EventoContingenciaId",
                table: "lotes");

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_doc_CatTipoDocResponsableId",
                table: "eventos_contingencia",
                column: "CatTipoDocResponsableId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
