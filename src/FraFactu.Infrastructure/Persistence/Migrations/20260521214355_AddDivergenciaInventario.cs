using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDivergenciaInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_DivergenciaInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EjecucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombreBodega = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StockSmartix = table.Column<int>(type: "integer", nullable: false),
                    StockSmartInventory = table.Column<int>(type: "integer", nullable: false),
                    Diff = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UltimaDeteccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_DivergenciaInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_DivergenciaInventario_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DivergenciaInventario_EjecucionId",
                table: "TBL_DivergenciaInventario",
                column: "EjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_DivergenciaInventario_Emisor_Producto_Bodega_Estado",
                table: "TBL_DivergenciaInventario",
                columns: new[] { "EmisorId", "CodigoProducto", "NombreBodega", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_DivergenciaInventario_Estado_UltimaDeteccion",
                table: "TBL_DivergenciaInventario",
                columns: new[] { "Estado", "UltimaDeteccion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_DivergenciaInventario");
        }
    }
}
