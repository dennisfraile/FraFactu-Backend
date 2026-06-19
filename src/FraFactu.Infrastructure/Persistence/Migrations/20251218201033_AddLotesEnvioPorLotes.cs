using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLotesEnvioPorLotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoteId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoLote = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDtes = table.Column<int>(type: "integer", nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    EsContingencia = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdEnvio = table.Column<Guid>(type: "uuid", nullable: true),
                    FhProcesamiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigoRespuesta = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DescripcionRespuesta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JsonRespuesta = table.Column<string>(type: "text", nullable: true),
                    JsonEnviado = table.Column<string>(type: "text", nullable: true),
                    TotalAprobados = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalRechazados = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalPendientes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreadoPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaUltimaConsulta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lotes_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lote_detalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoteId = table.Column<int>(type: "integer", nullable: false),
                    FacturaElectronicaId = table.Column<int>(type: "integer", nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    EstadoDte = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SelloRecibido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaConsulta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigoRechazo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ObservacionesRechazo = table.Column<string>(type: "text", nullable: true),
                    JsonRespuestaIndividual = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lote_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lote_detalles_Facturas_FacturaElectronicaId",
                        column: x => x.FacturaElectronicaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lote_detalles_lotes_LoteId",
                        column: x => x.LoteId,
                        principalTable: "lotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_LoteId",
                table: "Facturas",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteDetalle_Estado",
                table: "lote_detalles",
                column: "EstadoDte");

            migrationBuilder.CreateIndex(
                name: "IX_LoteDetalle_Factura",
                table: "lote_detalles",
                column: "FacturaElectronicaId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteDetalle_Lote",
                table: "lote_detalles",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteDetalle_Lote_Factura",
                table: "lote_detalles",
                columns: new[] { "LoteId", "FacturaElectronicaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_CodigoLote",
                table: "lotes",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_Emisor",
                table: "lotes",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_Estado",
                table: "lotes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_FechaCreacion",
                table: "lotes",
                column: "FechaCreacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_lotes_LoteId",
                table: "Facturas",
                column: "LoteId",
                principalTable: "lotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_lotes_LoteId",
                table: "Facturas");

            migrationBuilder.DropTable(
                name: "lote_detalles");

            migrationBuilder.DropTable(
                name: "lotes");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_LoteId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "LoteId",
                table: "Facturas");
        }
    }
}
