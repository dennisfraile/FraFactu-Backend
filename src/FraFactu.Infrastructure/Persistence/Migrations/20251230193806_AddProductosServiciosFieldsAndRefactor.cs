using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductosServiciosFieldsAndRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compra_externa_detalles_productos_ProductoId",
                table: "compra_externa_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoServicioId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_productos_ProductoId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_doc_CatTipoDocumento~",
                table: "FacturaDocumentosRelacionados");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_generacion_CatTipoGe~",
                table: "FacturaDocumentosRelacionados");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaExtenciones_Facturas_FacturaId",
                table: "FacturaExtenciones");

            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_inventario_productos_ProductoId",
                table: "movimientos_inventario");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_bodegas_productos_ProductoId",
                table: "stock_bodegas");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropIndex(
                name: "IX_factura_detalles_ProductoServicioId",
                table: "factura_detalles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FacturaExtenciones",
                table: "FacturaExtenciones");

            migrationBuilder.DropColumn(
                name: "ProductoServicioId",
                table: "factura_detalles");

            migrationBuilder.RenameTable(
                name: "FacturaExtenciones",
                newName: "FacturaExtensiones");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaExtenciones_FacturaId",
                table: "FacturaExtensiones",
                newName: "IX_FacturaExtensiones_FacturaId");

            migrationBuilder.AlterColumn<int>(
                name: "EmisorId",
                table: "Usuarios",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "AplicaIVA",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: true,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoBarras",
                table: "TBL_ProductosServicios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PermiteVentaSinStock",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeIVA",
                table: "TBL_ProductosServicios",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                defaultValue: 13m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCosto",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PuntoReorden",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockMaximo",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimo",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "FacturaDocumentosRelacionados",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Valor",
                table: "FacturaApendices",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Etiqueta",
                table: "FacturaApendices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Campo",
                table: "FacturaApendices",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PlacaVehiculo",
                table: "FacturaExtensiones",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "FacturaExtensiones",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombRecibe",
                table: "FacturaExtensiones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombEntrega",
                table: "FacturaExtensiones",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocuRecibe",
                table: "FacturaExtensiones",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocuEntrega",
                table: "FacturaExtensiones",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FacturaExtensiones",
                table: "FacturaExtensiones",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Categoria",
                table: "TBL_ProductosServicios",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoBarras",
                table: "TBL_ProductosServicios",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Marca",
                table: "TBL_ProductosServicios",
                column: "MarcaId");

            migrationBuilder.AddForeignKey(
                name: "FK_compra_externa_detalles_TBL_ProductosServicios_ProductoId",
                table: "compra_externa_detalles",
                column: "ProductoId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoId",
                table: "factura_detalles",
                column: "ProductoId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_doc_CatTipoDocumento~",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoDocumentoId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_generacion_CatTipoGe~",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoGeneracionDocumentoId",
                principalTable: "cat_tipo_generacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaExtensiones_Facturas_FacturaId",
                table: "FacturaExtensiones",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_inventario_TBL_ProductosServicios_ProductoId",
                table: "movimientos_inventario",
                column: "ProductoId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_bodegas_TBL_ProductosServicios_ProductoId",
                table: "stock_bodegas",
                column: "ProductoId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ProductosServicios_categorias_CategoriaId",
                table: "TBL_ProductosServicios",
                column: "CategoriaId",
                principalTable: "categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ProductosServicios_marcas_MarcaId",
                table: "TBL_ProductosServicios",
                column: "MarcaId",
                principalTable: "marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compra_externa_detalles_TBL_ProductosServicios_ProductoId",
                table: "compra_externa_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoId",
                table: "factura_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_doc_CatTipoDocumento~",
                table: "FacturaDocumentosRelacionados");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_generacion_CatTipoGe~",
                table: "FacturaDocumentosRelacionados");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturaExtensiones_Facturas_FacturaId",
                table: "FacturaExtensiones");

            migrationBuilder.DropForeignKey(
                name: "FK_movimientos_inventario_TBL_ProductosServicios_ProductoId",
                table: "movimientos_inventario");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_bodegas_TBL_ProductosServicios_ProductoId",
                table: "stock_bodegas");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_categorias_CategoriaId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_marcas_MarcaId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Categoria",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CodigoBarras",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Marca",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FacturaExtensiones",
                table: "FacturaExtensiones");

            migrationBuilder.DropColumn(
                name: "AplicaIVA",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "CodigoBarras",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PermiteVentaSinStock",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PorcentajeIVA",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PrecioCosto",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PuntoReorden",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "StockMaximo",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "TBL_ProductosServicios");

            migrationBuilder.RenameTable(
                name: "FacturaExtensiones",
                newName: "FacturaExtenciones");

            migrationBuilder.RenameIndex(
                name: "IX_FacturaExtensiones_FacturaId",
                table: "FacturaExtenciones",
                newName: "IX_FacturaExtenciones_FacturaId");

            migrationBuilder.AlterColumn<int>(
                name: "EmisorId",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "FacturaDocumentosRelacionados",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "Valor",
                table: "FacturaApendices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Etiqueta",
                table: "FacturaApendices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Campo",
                table: "FacturaApendices",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25);

            migrationBuilder.AddColumn<int>(
                name: "ProductoServicioId",
                table: "factura_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlacaVehiculo",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombRecibe",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombEntrega",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocuRecibe",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocuEntrega",
                table: "FacturaExtenciones",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FacturaExtenciones",
                table: "FacturaExtenciones",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    CatUnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    MarcaId = table.Column<int>(type: "integer", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AplicaIVA = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EsServicio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PermiteVentaSinStock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PorcentajeIVA = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 13m),
                    PrecioCosto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PuntoReorden = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    StockMaximo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
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

            migrationBuilder.CreateIndex(
                name: "IX_factura_detalles_ProductoServicioId",
                table: "factura_detalles",
                column: "ProductoServicioId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_compra_externa_detalles_productos_ProductoId",
                table: "compra_externa_detalles",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_TBL_ProductosServicios_ProductoServicioId",
                table: "factura_detalles",
                column: "ProductoServicioId",
                principalTable: "TBL_ProductosServicios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_factura_detalles_productos_ProductoId",
                table: "factura_detalles",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_doc_CatTipoDocumento~",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoDocumentoId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDocumentosRelacionados_cat_tipo_generacion_CatTipoGe~",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoGeneracionDocumentoId",
                principalTable: "cat_tipo_generacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaExtenciones_Facturas_FacturaId",
                table: "FacturaExtenciones",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_movimientos_inventario_productos_ProductoId",
                table: "movimientos_inventario",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_bodegas_productos_ProductoId",
                table: "stock_bodegas",
                column: "ProductoId",
                principalTable: "productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
