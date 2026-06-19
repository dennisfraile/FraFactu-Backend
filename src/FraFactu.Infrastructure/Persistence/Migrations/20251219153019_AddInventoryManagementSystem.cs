using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_Facturas_FacturaId",
                table: "FacturaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_TBL_ProductosServicios_ProductoServicioId",
                table: "FacturaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_cat_tipo_item_CatTipoItemId",
                table: "FacturaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_cat_uni_medida_CatUnidadMedidaId",
                table: "FacturaDetalles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FacturaDetalles",
                table: "FacturaDetalles");

            migrationBuilder.RenameTable(
                name: "FacturaDetalles",
                newName: "factura_detalles");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_ProductoServicioId",
                table: "factura_detalles",
                newName: "IX_factura_detalles_ProductoServicioId");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_FacturaId",
                table: "factura_detalles",
                newName: "IX_factura_detalles_FacturaId");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_CatUnidadMedidaId",
                table: "factura_detalles",
                newName: "IX_factura_detalles_CatUnidadMedidaId");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaDetalles_CatTipoItemId",
                table: "factura_detalles",
                newName: "IX_factura_detalles_CatTipoItemId");

            migrationBuilder.AddColumn<int>(
                name: "BodegaId",
                table: "factura_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoUnitario",
                table: "factura_detalles",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "factura_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_factura_detalles",
                table: "factura_detalles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "bodegas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bodegas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bodegas_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoriaPadreId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categorias_categorias_CategoriaPadreId",
                        column: x => x.CategoriaPadreId,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    MarcaId = table.Column<int>(type: "integer", nullable: true),
                    CatUnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    StockMaximo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PuntoReorden = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AplicaIVA = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PorcentajeIVA = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 13m),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PermiteVentaSinStock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EsServicio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_productos_cat_uni_medida_CatUnidadMedidaId",
                        column: x => x.CatUnidadMedidaId,
                        principalTable: "cat_uni_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimientos_inventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    BodegaId = table.Column<int>(type: "integer", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentoId = table.Column<int>(type: "integer", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BodegaDestinoId = table.Column<int>(type: "integer", nullable: true),
                    FechaMovimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UsuarioRegistro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SaldoAnterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NuevoSaldo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CostoPromedioAnterior = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    NuevoCostoPromedio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_inventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_bodegas_BodegaDestinoId",
                        column: x => x.BodegaDestinoId,
                        principalTable: "bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalTable: "bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimientos_inventario_productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_bodegas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    BodegaId = table.Column<int>(type: "integer", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CantidadReservada = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CostoPromedio = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    UltimaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_bodegas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_bodegas_bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalTable: "bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_bodegas_productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_Bodega",
                table: "factura_detalles",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_Producto",
                table: "factura_detalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Codigo",
                table: "bodegas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Nombre",
                table: "bodegas",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_Sucursal",
                table: "bodegas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_CategoriaPadreId",
                table: "categorias",
                column: "CategoriaPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Codigo",
                table: "categorias",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "categorias",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre",
                table: "marcas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_inventario_BodegaDestinoId",
                table: "movimientos_inventario",
                column: "BodegaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Bodega",
                table: "movimientos_inventario",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Documento",
                table: "movimientos_inventario",
                columns: new[] { "TipoDocumento", "DocumentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Fecha",
                table: "movimientos_inventario",
                column: "FechaMovimiento");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Producto",
                table: "movimientos_inventario",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Producto_Bodega_Fecha",
                table: "movimientos_inventario",
                columns: new[] { "ProductoId", "BodegaId", "FechaMovimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Tipo",
                table: "movimientos_inventario",
                column: "TipoMovimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Activo",
                table: "productos",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Categoria",
                table: "productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_productos_CatUnidadMedidaId",
                table: "productos",
                column: "CatUnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Codigo",
                table: "productos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "productos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Marca",
                table: "productos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Nombre",
                table: "productos",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_Bodega",
                table: "stock_bodegas",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_Producto",
                table: "stock_bodegas",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_Producto_Bodega",
                table: "stock_bodegas",
                columns: new[] { "ProductoId", "BodegaId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_Facturas_FacturaId",
                table: "factura_detalles",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoServicioId",
                table: "factura_detalles",
                column: "ProductoServicioId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_bodegas_BodegaId",
                table: "factura_detalles",
                column: "BodegaId",
                principalTable: "bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_cat_tipo_item_CatTipoItemId",
                table: "factura_detalles",
                column: "CatTipoItemId",
                principalTable: "cat_tipo_item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_cat_uni_medida_CatUnidadMedidaId",
                table: "factura_detalles",
                column: "CatUnidadMedidaId",
                principalTable: "cat_uni_medida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_productos_ProductoId",
                table: "factura_detalles",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_Facturas_FacturaId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoServicioId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_bodegas_BodegaId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_cat_tipo_item_CatTipoItemId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_cat_uni_medida_CatUnidadMedidaId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_productos_ProductoId",
                table: "factura_detalles");

            migrationBuilder.DropTable(
                name: "movimientos_inventario");

            migrationBuilder.DropTable(
                name: "stock_bodegas");

            migrationBuilder.DropTable(
                name: "bodegas");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "marcas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_factura_detalles",
                table: "factura_detalles");

            migrationBuilder.DropIndex(
                name: "IX_FacturaDetalles_Bodega",
                table: "factura_detalles");

            migrationBuilder.DropIndex(
                name: "IX_FacturaDetalles_Producto",
                table: "factura_detalles");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "factura_detalles");

            migrationBuilder.DropColumn(
                name: "CostoUnitario",
                table: "factura_detalles");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "factura_detalles");

            migrationBuilder.RenameTable(
                name: "factura_detalles",
                newName: "FacturaDetalles");

            migrationBuilder.RenameIndex(
                name: "IX_factura_detalles_ProductoServicioId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_ProductoServicioId");

            migrationBuilder.RenameIndex(
                name: "IX_factura_detalles_FacturaId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_FacturaId");

            migrationBuilder.RenameIndex(
                name: "IX_factura_detalles_CatUnidadMedidaId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_CatUnidadMedidaId");

            migrationBuilder.RenameIndex(
                name: "IX_factura_detalles_CatTipoItemId",
                table: "FacturaDetalles",
                newName: "IX_FacturaDetalles_CatTipoItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FacturaDetalles",
                table: "FacturaDetalles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_Facturas_FacturaId",
                table: "FacturaDetalles",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_TBL_ProductosServicios_ProductoServicioId",
                table: "FacturaDetalles",
                column: "ProductoServicioId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_cat_tipo_item_CatTipoItemId",
                table: "FacturaDetalles",
                column: "CatTipoItemId",
                principalTable: "cat_tipo_item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_cat_uni_medida_CatUnidadMedidaId",
                table: "FacturaDetalles",
                column: "CatUnidadMedidaId",
                principalTable: "cat_uni_medida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
