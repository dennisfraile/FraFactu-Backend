using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F1_LimpiezaHubSmartCareInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaPrefills");

            migrationBuilder.DropTable(
                name: "migraciones_inventario_procesadas");

            migrationBuilder.DropTable(
                name: "TBL_DivergenciaInventario");

            migrationBuilder.DropTable(
                name: "TBL_IntegracionInventarioPendiente");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_HubUsuarioId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Sucursal_HubSucursalId",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_HubId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "HubUsuarioId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "HubSucursalId",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "HubId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "TieneSmartInventoryActiva",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "SmartCareClinicId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareCorrelationId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareVisitId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareWebhookUrl",
                table: "Facturas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HubUsuarioId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HubSucursalId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HubId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneSmartInventoryActiva",
                table: "TBL_Emisores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareClinicId",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareCorrelationId",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareVisitId",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareWebhookUrl",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacturaPrefills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsumedFacturaId = table.Column<int>(type: "integer", nullable: true),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FormaPagoSugerida = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    LineasJson = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ReceptorJson = table.Column<string>(type: "text", nullable: false),
                    SmartCareClinicId = table.Column<string>(type: "text", nullable: true),
                    SmartCareVisitId = table.Column<string>(type: "text", nullable: true),
                    SmartCareWebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnapshotFiscalJson = table.Column<string>(type: "text", nullable: true),
                    SucursalSmartixId = table.Column<int>(type: "integer", nullable: false),
                    TipoDte = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaPrefills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaPrefills_Facturas_ConsumedFacturaId",
                        column: x => x.ConsumedFacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturaPrefills_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "migraciones_inventario_procesadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HubId = table.Column<int>(type: "integer", nullable: false),
                    MigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcesadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migraciones_inventario_procesadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_migraciones_inventario_procesadas_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TBL_DivergenciaInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoProducto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Diff = table.Column<int>(type: "integer", nullable: false),
                    EjecucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NombreBodega = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StockSmartInventory = table.Column<int>(type: "integer", nullable: false),
                    StockSmartix = table.Column<int>(type: "integer", nullable: false),
                    UltimaDeteccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "TBL_IntegracionInventarioPendiente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaProximoIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    MovimientoIdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    TipoEvento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBL_IntegracionInventarioPendiente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBL_IntegracionInventarioPendiente_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_HubUsuarioId",
                table: "Usuarios",
                column: "HubUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_HubSucursalId",
                table: "TBL_Sucursales",
                column: "HubSucursalId",
                unique: true,
                filter: "\"HubSucursalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_HubId",
                table: "TBL_Emisores",
                column: "HubId",
                unique: true,
                filter: "\"HubId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPrefills_ConsumedFacturaId",
                table: "FacturaPrefills",
                column: "ConsumedFacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPrefills_CorrelationId",
                table: "FacturaPrefills",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPrefills_EmisorId",
                table: "FacturaPrefills",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPrefills_ExpiresAt",
                table: "FacturaPrefills",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_migraciones_inventario_procesadas_EmisorId",
                table: "migraciones_inventario_procesadas",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_migraciones_inventario_procesadas_MigrationId_Tipo",
                table: "migraciones_inventario_procesadas",
                columns: new[] { "MigrationId", "Tipo" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_IntegracionInventario_Estado_ProximoIntento",
                table: "TBL_IntegracionInventarioPendiente",
                columns: new[] { "Estado", "FechaProximoIntento", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegracionInventario_MovimientoIdExterno",
                table: "TBL_IntegracionInventarioPendiente",
                column: "MovimientoIdExterno");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_IntegracionInventarioPendiente_EmisorId",
                table: "TBL_IntegracionInventarioPendiente",
                column: "EmisorId");
        }
    }
}
