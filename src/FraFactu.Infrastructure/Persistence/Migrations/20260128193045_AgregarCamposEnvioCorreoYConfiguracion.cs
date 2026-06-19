using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposEnvioCorreoYConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CorreoEnviado",
                table: "Facturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailReceptor",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnvioCorreo",
                table: "Facturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionEnvioLotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    HoraEnvioAutomatico = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EnvioAutomaticoHabilitado = table.Column<bool>(type: "boolean", nullable: false),
                    ZonaHoraria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnviarRecordatorio = table.Column<bool>(type: "boolean", nullable: false),
                    MinutosAnticipacionRecordatorio = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionEnvioLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionEnvioLotes_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionEnvioLotes_EmisorId",
                table: "ConfiguracionEnvioLotes",
                column: "EmisorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionEnvioLotes");

            migrationBuilder.DropColumn(
                name: "CorreoEnviado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "EmailReceptor",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FechaEnvioCorreo",
                table: "Facturas");
        }
    }
}
