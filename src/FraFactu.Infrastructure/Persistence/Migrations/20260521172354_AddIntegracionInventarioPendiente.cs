using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegracionInventarioPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBL_IntegracionInventarioPendiente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoEvento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaProximoIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaProcesado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MovimientoIdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBL_IntegracionInventarioPendiente");
        }
    }
}
