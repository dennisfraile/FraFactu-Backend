using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoContingencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_contingencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    Ambiente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "00"),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FechaTransmision = table.Column<DateTime>(type: "date", nullable: false),
                    HoraTransmision = table.Column<TimeSpan>(type: "time", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    NombreResponsable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoDocResponsable = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumeroDocResponsable = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    TipoEstablecimiento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CodigoEstablecimientoMH = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CodigoPuntoVenta = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    FechaInicioContingencia = table.Column<DateTime>(type: "date", nullable: false),
                    FechaFinContingencia = table.Column<DateTime>(type: "date", nullable: false),
                    HoraInicioContingencia = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFinContingencia = table.Column<TimeSpan>(type: "time", nullable: false),
                    TipoContingencia = table.Column<int>(type: "integer", nullable: false),
                    MotivoContingencia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_eventos_contingencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_contingencia_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contingencia_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoContingenciaId = table.Column<int>(type: "integer", nullable: false),
                    FacturaElectronicaId = table.Column<int>(type: "integer", nullable: false),
                    NoItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contingencia_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contingencia_detalles_Facturas_FacturaElectronicaId",
                        column: x => x.FacturaElectronicaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contingencia_detalles_eventos_contingencia_EventoContingenc~",
                        column: x => x.EventoContingenciaId,
                        principalTable: "eventos_contingencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContingenciaDetalles_Evento_Factura",
                table: "contingencia_detalles",
                columns: new[] { "EventoContingenciaId", "FacturaElectronicaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContingenciaDetalles_Evento_NoItem",
                table: "contingencia_detalles",
                columns: new[] { "EventoContingenciaId", "NoItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContingenciaDetalles_EventoId",
                table: "contingencia_detalles",
                column: "EventoContingenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContingenciaDetalles_FacturaId",
                table: "contingencia_detalles",
                column: "FacturaElectronicaId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_CodigoGeneracion",
                table: "eventos_contingencia",
                column: "CodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_Emisor",
                table: "eventos_contingencia",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_Emisor_Fecha",
                table: "eventos_contingencia",
                columns: new[] { "EmisorId", "FechaTransmision" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_FechaTransmision",
                table: "eventos_contingencia",
                column: "FechaTransmision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contingencia_detalles");

            migrationBuilder.DropTable(
                name: "eventos_contingencia");
        }
    }
}
