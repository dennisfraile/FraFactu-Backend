using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComprasExternasYGastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RevirtiInventario",
                table: "invalidaciones",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoInvalidacion",
                table: "invalidaciones",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cat_tipo_gasto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_tipo_gasto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NIT = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NombreComercial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Contacto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SitioWeb = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "compras_externas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    NumeroFactura = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IVA = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "BORRADOR"),
                    FechaConfirmacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_externas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compras_externas_proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compra_externa_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraExternaId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    BodegaId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IVA = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EsParaInventario = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_externa_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compra_externa_detalles_bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalTable: "bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compra_externa_detalles_compras_externas_CompraExternaId",
                        column: x => x.CompraExternaId,
                        principalTable: "compras_externas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_compra_externa_detalles_productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gastos_administrativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraExternaId = table.Column<int>(type: "integer", nullable: false),
                    CatTipoGastoId = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CentroCosto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CuentaContable = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gastos_administrativos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gastos_administrativos_cat_tipo_gasto_CatTipoGastoId",
                        column: x => x.CatTipoGastoId,
                        principalTable: "cat_tipo_gasto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gastos_administrativos_compras_externas_CompraExternaId",
                        column: x => x.CompraExternaId,
                        principalTable: "compras_externas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Codigo",
                table: "cat_tipo_gasto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Nombre",
                table: "cat_tipo_gasto",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_CompraExternaDetalles_Bodega",
                table: "compra_externa_detalles",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraExternaDetalles_CompraExterna",
                table: "compra_externa_detalles",
                column: "CompraExternaId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraExternaDetalles_Producto",
                table: "compra_externa_detalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasExternas_Estado",
                table: "compras_externas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasExternas_FechaEmision",
                table: "compras_externas",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasExternas_NumeroFactura",
                table: "compras_externas",
                column: "NumeroFactura");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasExternas_Proveedor",
                table: "compras_externas",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasExternas_Proveedor_NumeroFactura",
                table: "compras_externas",
                columns: new[] { "ProveedorId", "NumeroFactura" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GastosAdministrativos_CentroCosto",
                table: "gastos_administrativos",
                column: "CentroCosto");

            migrationBuilder.CreateIndex(
                name: "IX_GastosAdministrativos_CompraExterna",
                table: "gastos_administrativos",
                column: "CompraExternaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastosAdministrativos_TipoGasto",
                table: "gastos_administrativos",
                column: "CatTipoGastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Activo",
                table: "proveedores",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT",
                table: "proveedores",
                column: "NIT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "proveedores",
                column: "Nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compra_externa_detalles");

            migrationBuilder.DropTable(
                name: "gastos_administrativos");

            migrationBuilder.DropTable(
                name: "cat_tipo_gasto");

            migrationBuilder.DropTable(
                name: "compras_externas");

            migrationBuilder.DropTable(
                name: "proveedores");

            migrationBuilder.DropColumn(
                name: "RevirtiInventario",
                table: "invalidaciones");

            migrationBuilder.DropColumn(
                name: "TipoInvalidacion",
                table: "invalidaciones");
        }
    }
}
