using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoServicioTributos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "producto_servicio_tributos",
                columns: table => new
                {
                    ProductoServicioId = table.Column<int>(type: "integer", nullable: false),
                    CatTributoId = table.Column<int>(type: "integer", nullable: false),
                    TipoCalculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_servicio_tributos", x => new { x.ProductoServicioId, x.CatTributoId });
                    table.ForeignKey(
                        name: "FK_producto_servicio_tributos_TBL_ProductosServicios_ProductoS~",
                        column: x => x.ProductoServicioId,
                        principalTable: "TBL_ProductosServicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_producto_servicio_tributos_cat_tributos_CatTributoId",
                        column: x => x.CatTributoId,
                        principalTable: "cat_tributos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoServicioTributo_CatTributoId",
                table: "producto_servicio_tributos",
                column: "CatTributoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "producto_servicio_tributos");
        }
    }
}
