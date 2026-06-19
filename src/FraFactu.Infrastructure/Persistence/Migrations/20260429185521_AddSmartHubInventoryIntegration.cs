using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartHubInventoryIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HubId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "migraciones_inventario_procesadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MigrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HubId = table.Column<int>(type: "integer", nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    ProcesadaEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_migraciones_inventario_procesadas_EmisorId",
                table: "migraciones_inventario_procesadas",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_migraciones_inventario_procesadas_MigrationId_Tipo",
                table: "migraciones_inventario_procesadas",
                columns: new[] { "MigrationId", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "migraciones_inventario_procesadas");

            migrationBuilder.DropColumn(
                name: "HubId",
                table: "TBL_Emisores");
        }
    }
}
