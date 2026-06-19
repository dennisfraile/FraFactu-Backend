using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoRetorno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_retorno",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Ambiente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "00"),
                    TipoModelo = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TipoOperacion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TipoEvento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "18"),
                    TipoContingencia = table.Column<int>(type: "integer", nullable: true),
                    MotivoContin = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "date", nullable: false),
                    HoraEmision = table.Column<TimeSpan>(type: "time", nullable: false),
                    Fusion = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    TipoMoneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    CodEstableMH = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CodEstable = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CodPuntoVentaMH = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CodPuntoVenta = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    RecintoFiscal = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    TipoRegimen = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Regimen = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    TipoItemExpor = table.Column<int>(type: "integer", nullable: true),
                    DocumentoRelacionadoJson = table.Column<string>(type: "text", nullable: true),
                    DocumentoReceptorJson = table.Column<string>(type: "text", nullable: true),
                    VentaTerceroJson = table.Column<string>(type: "text", nullable: true),
                    CompraTerceroJson = table.Column<string>(type: "text", nullable: true),
                    ResumenTributosJson = table.Column<string>(type: "text", nullable: true),
                    ApendiceJson = table.Column<string>(type: "text", nullable: true),
                    TotalNoSuj = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalExenta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalGravada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCompraExcluidos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SubTotalVentas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalFlete = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MontoTotalOperacion = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IvaRete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReteRenta = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalNoGravado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPagar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalLetras = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalNoOnerosas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoFavor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaTransmisionMH = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelloRecibido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EstadoHacienda = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JsonEvento = table.Column<string>(type: "text", nullable: true),
                    JsonRespuesta = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_retorno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_retorno_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retorno_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoRetornoId = table.Column<int>(type: "integer", nullable: false),
                    NumItem = table.Column<int>(type: "integer", nullable: false),
                    TipoItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    PrecioUni = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    UniMedida = table.Column<int>(type: "integer", nullable: false),
                    MontoDescu = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    CodTributo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    VentaNoSuj = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    VentaExenta = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    VentaGravada = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Compra = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    TributosJson = table.Column<string>(type: "text", nullable: true),
                    Psv = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    IvaItem = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    NoGravado = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Seguro = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Flete = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    IvaRete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReteRenta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retorno_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_retorno_detalles_eventos_retorno_EventoRetornoId",
                        column: x => x.EventoRetornoId,
                        principalTable: "eventos_retorno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosRetorno_CodigoGeneracion_EmisorId",
                table: "eventos_retorno",
                columns: new[] { "CodigoGeneracion", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosRetorno_Emisor",
                table: "eventos_retorno",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosRetorno_FechaEmision",
                table: "eventos_retorno",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_RetornoDetalles_Evento_NumItem",
                table: "retorno_detalles",
                columns: new[] { "EventoRetornoId", "NumItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetornoDetalles_EventoId",
                table: "retorno_detalles",
                column: "EventoRetornoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retorno_detalles");

            migrationBuilder.DropTable(
                name: "eventos_retorno");
        }
    }
}
