using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistritoCAT008 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatDistritoId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatDistritoId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatDistritoId",
                table: "Receptores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cat_distrito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoDepartamento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CodigoMunicipio = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_distrito", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Sucursales_CatDistritoId",
                table: "TBL_Sucursales",
                column: "CatDistritoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_CatDistritoId",
                table: "TBL_Emisores",
                column: "CatDistritoId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptores_CatDistritoId",
                table: "Receptores",
                column: "CatDistritoId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_distrito_depto_muni_codigo",
                table: "cat_distrito",
                columns: new[] { "CodigoDepartamento", "CodigoMunicipio", "Codigo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_distrito_CatDistritoId",
                table: "Receptores",
                column: "CatDistritoId",
                principalTable: "cat_distrito",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Emisores_cat_distrito_CatDistritoId",
                table: "TBL_Emisores",
                column: "CatDistritoId",
                principalTable: "cat_distrito",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_cat_distrito_CatDistritoId",
                table: "TBL_Sucursales",
                column: "CatDistritoId",
                principalTable: "cat_distrito",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_distrito_CatDistritoId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Emisores_cat_distrito_CatDistritoId",
                table: "TBL_Emisores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_cat_distrito_CatDistritoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropTable(
                name: "cat_distrito");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Sucursales_CatDistritoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_CatDistritoId",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_Receptores_CatDistritoId",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "CatDistritoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CatDistritoId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CatDistritoId",
                table: "Receptores");
        }
    }
}
