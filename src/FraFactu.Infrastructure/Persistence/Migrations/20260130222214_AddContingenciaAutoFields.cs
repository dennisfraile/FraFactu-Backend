using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContingenciaAutoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetalleErrorEnvio",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventoContingenciaId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaErrorEnvio",
                table: "Facturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosEnvio",
                table: "Facturas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoContingenciaSugerido",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreadoAutomaticamente",
                table: "eventos_contingencia",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EventoContingenciaId",
                table: "Facturas",
                column: "EventoContingenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_eventos_contingencia_EventoContingenciaId",
                table: "Facturas",
                column: "EventoContingenciaId",
                principalTable: "eventos_contingencia",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_eventos_contingencia_EventoContingenciaId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_EventoContingenciaId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "DetalleErrorEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "EventoContingenciaId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FechaErrorEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "IntentosEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "TipoContingenciaSugerido",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CreadoAutomaticamente",
                table: "eventos_contingencia");
        }
    }
}
