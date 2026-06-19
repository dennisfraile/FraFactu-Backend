using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductoToProductoServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_TBL_Productos_ProductoId",
                table: "FacturaDetalles");

            migrationBuilder.DropTable(
                name: "TBL_Productos");

            migrationBuilder.RenameColumn(
                name: "ProductoId",
                table: "FacturaDetalles",
                newName: "ProductoServicioId");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_ProductoId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_ProductoServicioId");

            migrationBuilder.CreateTable(
                name: "TBL_ProductosServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CatUnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoItemId = table.Column<int>(type: "integer", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_ProductosServicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_ProductosServicios_cat_tipo_item_CatTipoItemId",
                        column: x => x.CatTipoItemId,
                        principalTable: "cat_tipo_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_ProductosServicios_cat_uni_medida_CatUnidadMedidaId",
                        column: x => x.CatUnidadMedidaId,
                        principalTable: "cat_uni_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_ProductosServicios_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor",
                table: "TBL_ProductosServicios",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor_Codigo",
                table: "TBL_ProductosServicios",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ProductosServicios_CatTipoItemId",
                table: "TBL_ProductosServicios",
                column: "CatTipoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_ProductosServicios_CatUnidadMedidaId",
                table: "TBL_ProductosServicios",
                column: "CatUnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_TBL_ProductosServicios_ProductoServicioId",
                table: "FacturaDetalles",
                column: "ProductoServicioId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_TBL_ProductosServicios_ProductoServicioId",
                table: "FacturaDetalles");

            migrationBuilder.DropTable(
                name: "TBL_ProductosServicios");

            migrationBuilder.RenameColumn(
                name: "ProductoServicioId",
                table: "FacturaDetalles",
                newName: "ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_ProductoServicioId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_ProductoId");

            migrationBuilder.CreateTable(
                name: "TBL_Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CatTipoItemId = table.Column<int>(type: "integer", nullable: false),
                    CatUnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_Productos_cat_tipo_item_CatTipoItemId",
                        column: x => x.CatTipoItemId,
                        principalTable: "cat_tipo_item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_Productos_cat_uni_medida_CatUnidadMedidaId",
                        column: x => x.CatUnidadMedidaId,
                        principalTable: "cat_uni_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TBL_Productos_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor",
                table: "TBL_Productos",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor_Codigo",
                table: "TBL_Productos",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Productos_CatTipoItemId",
                table: "TBL_Productos",
                column: "CatTipoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Productos_CatUnidadMedidaId",
                table: "TBL_Productos",
                column: "CatUnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_TBL_Productos_ProductoId",
                table: "FacturaDetalles",
                column: "ProductoId",
                principalTable: "TBL_Productos",
                principalColumn: "Id");
        }
    }
}
