using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoOperacionEspecial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_operacion_especial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Ambiente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "00"),
                    TipoModelo = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TipoOperacion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TipoEvento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "17"),
                    TipoMoneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "date", nullable: false),
                    HoraEmision = table.Column<TimeSpan>(type: "time", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    TotalNoSuj = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalExenta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalGravada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalLetras = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResumenTributosJson = table.Column<string>(type: "text", nullable: true),
                    ApendiceJson = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_eventos_operacion_especial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_operacion_especial_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operacion_especial_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoOperacionEspecialId = table.Column<int>(type: "integer", nullable: false),
                    NumItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoGeneracionRef = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TipoDocumento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumDocumento = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    FechaEmisionDoc = table.Column<DateTime>(type: "date", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    DocDel = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    DocAl = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PrecioUni = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    VentaNoSuj = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    VentaExenta = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    VentaGravada = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    TributosJson = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operacion_especial_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_operacion_especial_detalles_eventos_operacion_especial_Even~",
                        column: x => x.EventoOperacionEspecialId,
                        principalTable: "eventos_operacion_especial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosOperacionEspecial_CodigoGeneracion_EmisorId",
                table: "eventos_operacion_especial",
                columns: new[] { "CodigoGeneracion", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosOperacionEspecial_Emisor",
                table: "eventos_operacion_especial",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosOperacionEspecial_FechaEmision",
                table: "eventos_operacion_especial",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionEspecialDetalles_Evento_NumItem",
                table: "operacion_especial_detalles",
                columns: new[] { "EventoOperacionEspecialId", "NumItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperacionEspecialDetalles_EventoId",
                table: "operacion_especial_detalles",
                column: "EventoOperacionEspecialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operacion_especial_detalles");

            migrationBuilder.DropTable(
                name: "eventos_operacion_especial");
        }
    }
}
