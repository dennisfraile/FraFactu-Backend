using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionCuotasYRefinanciamiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracion_cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    TasaMoraMensual = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    DiasGracia = table.Column<int>(type: "integer", nullable: false),
                    MoraHabilitada = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_configuracion_cuotas_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_refinanciamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanCuotasId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PlanAnteriorJson = table.Column<string>(type: "jsonb", nullable: false),
                    PlanNuevoJson = table.Column<string>(type: "jsonb", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_refinanciamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historial_refinanciamiento_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historial_refinanciamiento_plan_cuotas_PlanCuotasId",
                        column: x => x.PlanCuotasId,
                        principalTable: "plan_cuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_cuotas_EmisorId",
                table: "configuracion_cuotas",
                column: "EmisorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_historial_refinanciamiento_PlanCuotasId",
                table: "historial_refinanciamiento",
                column: "PlanCuotasId");

            migrationBuilder.CreateIndex(
                name: "IX_historial_refinanciamiento_UsuarioId",
                table: "historial_refinanciamiento",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracion_cuotas");

            migrationBuilder.DropTable(
                name: "historial_refinanciamiento");
        }
    }
}
