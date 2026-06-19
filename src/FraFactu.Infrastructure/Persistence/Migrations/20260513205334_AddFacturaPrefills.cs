using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaPrefills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacturaPrefills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    SucursalSmartixId = table.Column<int>(type: "integer", nullable: false),
                    TipoDte = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ReceptorJson = table.Column<string>(type: "text", nullable: false),
                    LineasJson = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    FormaPagoSugerida = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    SmartCareClinicId = table.Column<int>(type: "integer", nullable: true),
                    SmartCareVisitId = table.Column<int>(type: "integer", nullable: true),
                    SmartCareWebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedFacturaId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaPrefills");
        }
    }
}
