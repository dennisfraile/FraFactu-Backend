using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoSucursalMoneda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Receptor_ReceptorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptor_emisores_EmisorId",
                table: "Receptor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Receptor",
                table: "Receptor");

            migrationBuilder.RenameTable(
                name: "Receptor",
                newName: "Receptores");

            migrationBuilder.RenameIndex(
                name: "IX_Receptor_EmisorId",
                table: "Receptores",
                newName: "IX_Receptores_EmisorId");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "FacturaDetalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Receptores",
                table: "Receptores",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CAT_Moneda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Simbolo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Valor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CAT_Moneda", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturaApendices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    Campo = table.Column<string>(type: "text", nullable: false),
                    Etiqueta = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaApendices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaApendices_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaDocumentosRelacionados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoDocumentoId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoGeneracionDocumentoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "text", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaDocumentosRelacionados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaDocumentosRelacionados_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDocumentosRelacionados_cat_tipo_doc_CatTipoDocumento~",
                        column: x => x.CatTipoDocumentoId,
                        principalTable: "cat_tipo_doc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDocumentosRelacionados_cat_tipo_generacion_CatTipoGe~",
                        column: x => x.CatTipoGeneracionDocumentoId,
                        principalTable: "cat_tipo_generacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturaExtenciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaId = table.Column<int>(type: "integer", nullable: false),
                    NombEntrega = table.Column<string>(type: "text", nullable: true),
                    DocuEntrega = table.Column<string>(type: "text", nullable: true),
                    NombRecibe = table.Column<string>(type: "text", nullable: true),
                    DocuRecibe = table.Column<string>(type: "text", nullable: true),
                    PlacaVehiculo = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaExtenciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaExtenciones_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TBL_Productos",
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

            migrationBuilder.CreateTable(
                name: "TBL_Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_Sucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_Sucursales_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CAT_Moneda",
                columns: new[] { "Id", "Codigo", "Simbolo", "Valor" },
                values: new object[,]
                {
                    { 1, "USD", "$", "Dólar estadounidense" },
                    { 2, "EUR", "€", "Euro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_SucursalId",
                table: "Facturas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_ProductoId",
                table: "FacturaDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_CatMoneda_Codigo",
                table: "CAT_Moneda",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturaApendices_FacturaId",
                table: "FacturaApendices",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDocumentosRelacionados_CatTipoDocumentoId",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDocumentosRelacionados_CatTipoGeneracionDocumentoId",
                table: "FacturaDocumentosRelacionados",
                column: "CatTipoGeneracionDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDocumentosRelacionados_FacturaId",
                table: "FacturaDocumentosRelacionados",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaExtenciones_FacturaId",
                table: "FacturaExtenciones",
                column: "FacturaId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_Emisor",
                table: "TBL_Sucursales",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_Emisor_Codigo",
                table: "TBL_Sucursales",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaDetalles_TBL_Productos_ProductoId",
                table: "FacturaDetalles",
                column: "ProductoId",
                principalTable: "TBL_Productos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas",
                column: "ReceptorId",
                principalTable: "Receptores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_emisores_EmisorId",
                table: "Receptores",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaDetalles_TBL_Productos_ProductoId",
                table: "FacturaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_emisores_EmisorId",
                table: "Receptores");

            migrationBuilder.DropTable(
                name: "CAT_Moneda");

            migrationBuilder.DropTable(
                name: "FacturaApendices");

            migrationBuilder.DropTable(
                name: "FacturaDocumentosRelacionados");

            migrationBuilder.DropTable(
                name: "FacturaExtenciones");

            migrationBuilder.DropTable(
                name: "TBL_Productos");

            migrationBuilder.DropTable(
                name: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_SucursalId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_FacturaDetalles_ProductoId",
                table: "FacturaDetalles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Receptores",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "FacturaDetalles");

            migrationBuilder.RenameTable(
                name: "Receptores",
                newName: "Receptor");

            migrationBuilder.RenameIndex(
                name: "IX_Receptores_EmisorId",
                table: "Receptor",
                newName: "IX_Receptor_EmisorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Receptor",
                table: "Receptor",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Receptor_ReceptorId",
                table: "Facturas",
                column: "ReceptorId",
                principalTable: "Receptor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptor_emisores_EmisorId",
                table: "Receptor",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
