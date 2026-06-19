using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInvalidacionToFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDocReceptor",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "TipoDocResponsable",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "TipoDocSolicita",
                table: "invalidaciones");

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocReceptorId",
                table: "invalidaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocResponsableId",
                table: "invalidaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocSolicitaId",
                table: "invalidaciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrar datos existentes: setear IDs válidos por defecto
            // (probablemente no hay datos de invalidación aún, pero por si acaso)
            migrationBuilder.Sql(@"
                UPDATE ""invalidaciones""
                SET ""CatTipoDocResponsableId"" = 2,  -- 2 = DUI por defecto
                    ""CatTipoDocSolicitaId"" = 2,      -- 2 = DUI por defecto
                    ""CatTipoDocReceptorId"" = 1       -- 1 = NIT por defecto
                WHERE ""CatTipoDocResponsableId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_invalidaciones_CatTipoDocReceptorId",
                table: "invalidaciones",
                column: "CatTipoDocReceptorId");

            migrationBuilder.CreateIndex(
                name: "IX_invalidaciones_CatTipoDocResponsableId",
                table: "invalidaciones",
                column: "CatTipoDocResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_invalidaciones_CatTipoDocSolicitaId",
                table: "invalidaciones",
                column: "CatTipoDocSolicitaId");

            migrationBuilder.AddForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocReceptorId",
                table: "invalidaciones",
                column: "CatTipoDocReceptorId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocResponsableId",
                table: "invalidaciones",
                column: "CatTipoDocResponsableId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocSolicitaId",
                table: "invalidaciones",
                column: "CatTipoDocSolicitaId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocReceptorId",
                table: "invalidaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocResponsableId",
                table: "invalidaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_invalidaciones_cat_tipo_doc_CatTipoDocSolicitaId",
                table: "invalidaciones");

            migrationBuilder.DropIndex(
                name: "IX_invalidaciones_CatTipoDocReceptorId",
                table: "invalidaciones");

            migrationBuilder.DropIndex(
                name: "IX_invalidaciones_CatTipoDocResponsableId",
                table: "invalidaciones");

            migrationBuilder.DropIndex(
                name: "IX_invalidaciones_CatTipoDocSolicitaId",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "CatTipoDocReceptorId",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "CatTipoDocResponsableId",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "CatTipoDocSolicitaId",
                table: "invalidaciones");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocReceptor",
                table: "invalidaciones",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocResponsable",
                table: "invalidaciones",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocSolicita",
                table: "invalidaciones",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }
    }
}
