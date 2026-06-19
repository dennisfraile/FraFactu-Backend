using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEventoContingenciaToFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDocResponsable",
                table: "eventos_contingencia");

            migrationBuilder.DropColumn(
                name: "TipoEstablecimiento",
                table: "eventos_contingencia");

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocResponsableId",
                table: "eventos_contingencia",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoEstablecimientoId",
                table: "eventos_contingencia",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrar datos existentes: setear IDs válidos en registros existentes
            // (probablemente no hay datos, pero por si acaso)
            migrationBuilder.Sql(@"
                UPDATE ""eventos_contingencia""
                SET ""CatTipoDocResponsableId"" = 1,  -- 1 = NIT por defecto
                    ""CatTipoEstablecimientoId"" = 1  -- 1 = Casa Matriz
                WHERE ""CatTipoDocResponsableId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_contingencia_CatTipoDocResponsableId",
                table: "eventos_contingencia",
                column: "CatTipoDocResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_contingencia_CatTipoEstablecimientoId",
                table: "eventos_contingencia",
                column: "CatTipoEstablecimientoId");

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_doc_CatTipoDocResponsableId",
                table: "eventos_contingencia",
                column: "CatTipoDocResponsableId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_establecimiento_CatTipoEstabl~",
                table: "eventos_contingencia",
                column: "CatTipoEstablecimientoId",
                principalTable: "cat_tipo_establecimiento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_doc_CatTipoDocResponsableId",
                table: "eventos_contingencia");

            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_cat_tipo_establecimiento_CatTipoEstabl~",
                table: "eventos_contingencia");

            migrationBuilder.DropIndex(
                name: "IX_eventos_contingencia_CatTipoDocResponsableId",
                table: "eventos_contingencia");

            migrationBuilder.DropIndex(
                name: "IX_eventos_contingencia_CatTipoEstablecimientoId",
                table: "eventos_contingencia");

            migrationBuilder.DropColumn(
                name: "CatTipoDocResponsableId",
                table: "eventos_contingencia");

            migrationBuilder.DropColumn(
                name: "CatTipoEstablecimientoId",
                table: "eventos_contingencia");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocResponsable",
                table: "eventos_contingencia",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoEstablecimiento",
                table: "eventos_contingencia",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }
    }
}
