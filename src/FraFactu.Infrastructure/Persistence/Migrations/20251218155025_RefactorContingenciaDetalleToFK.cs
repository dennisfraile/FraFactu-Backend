using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorContingenciaDetalleToFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "contingencia_detalles");

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocumentoId",
                table: "contingencia_detalles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrar datos existentes: usar NIT (1) por defecto
            // Nota: Este campo es solo una copia para referencia en JSON
            migrationBuilder.Sql(@"
                UPDATE ""contingencia_detalles""
                SET ""CatTipoDocumentoId"" = 1
                WHERE ""CatTipoDocumentoId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_contingencia_detalles_CatTipoDocumentoId",
                table: "contingencia_detalles",
                column: "CatTipoDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_contingencia_detalles_cat_tipo_doc_CatTipoDocumentoId",
                table: "contingencia_detalles",
                column: "CatTipoDocumentoId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contingencia_detalles_cat_tipo_doc_CatTipoDocumentoId",
                table: "contingencia_detalles");

            migrationBuilder.DropIndex(
                name: "IX_contingencia_detalles_CatTipoDocumentoId",
                table: "contingencia_detalles");

            migrationBuilder.DropColumn(
                name: "CatTipoDocumentoId",
                table: "contingencia_detalles");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "contingencia_detalles",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }
    }
}
