using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvalidacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invalidaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FechaAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraAnulacion = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    FacturaElectronicaId = table.Column<int>(type: "integer", nullable: false),
                    FacturaReemplazoId = table.Column<int>(type: "integer", nullable: true),
                    CodigoGeneracionReemplazo = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TipoAnulacion = table.Column<int>(type: "integer", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    NombreResponsable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoDocResponsable = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumDocResponsable = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreSolicita = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoDocSolicita = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumDocSolicita = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TipoDocReceptor = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumDocReceptor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreReceptor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TelefonoReceptor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CorreoReceptor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MontoIva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FechaTransmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SelloRecibido = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EstadoHacienda = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JsonEvento = table.Column<string>(type: "text", nullable: true),
                    JsonRespuesta = table.Column<string>(type: "text", nullable: true),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invalidaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invalidaciones_Facturas_FacturaElectronicaId",
                        column: x => x.FacturaElectronicaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invalidaciones_Facturas_FacturaReemplazoId",
                        column: x => x.FacturaReemplazoId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invalidaciones_emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_CodigoGeneracion",
                table: "invalidaciones",
                column: "CodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_Emisor_Fecha",
                table: "invalidaciones",
                columns: new[] { "EmisorId", "FechaAnulacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_FacturaElectronicaId",
                table: "invalidaciones",
                column: "FacturaElectronicaId");

            migrationBuilder.CreateIndex(
                name: "IX_invalidaciones_FacturaReemplazoId",
                table: "invalidaciones",
                column: "FacturaReemplazoId");

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_FechaAnulacion",
                table: "invalidaciones",
                column: "FechaAnulacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invalidaciones");
        }
    }
}
