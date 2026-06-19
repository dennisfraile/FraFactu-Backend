using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanCuotasYCuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    ReceptorId = table.Column<int>(type: "integer", nullable: true),
                    CondicionOperacion = table.Column<int>(type: "integer", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoAdeudado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstadoCobro = table.Column<int>(type: "integer", nullable: false),
                    VentaSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_cuotas_Receptores_ReceptorId",
                        column: x => x.ReceptorId,
                        principalTable: "Receptores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_cuotas_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanCuotasId = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    FechaPactada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FacturaId = table.Column<int>(type: "integer", nullable: true),
                    EsCuotaFinal = table.Column<bool>(type: "boolean", nullable: false),
                    InteresMora = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cuotas_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuotas_plan_cuotas_PlanCuotasId",
                        column: x => x.PlanCuotasId,
                        principalTable: "plan_cuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_Estado_FechaPactada",
                table: "cuotas",
                columns: new[] { "Estado", "FechaPactada" });

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_FacturaId",
                table: "cuotas",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_Plan_Numero",
                table: "cuotas",
                columns: new[] { "PlanCuotasId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_cuotas_Emisor_Estado",
                table: "plan_cuotas",
                columns: new[] { "EmisorId", "EstadoCobro" });

            migrationBuilder.CreateIndex(
                name: "IX_plan_cuotas_ReceptorId",
                table: "plan_cuotas",
                column: "ReceptorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cuotas");

            migrationBuilder.DropTable(
                name: "plan_cuotas");
        }
    }
}
