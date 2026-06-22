using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F3_SoftDeleteAuditadoProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DesactivadoEn",
                table: "TBL_ProductosServicios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DesactivadoPorUsuarioId",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDesactivacion",
                table: "TBL_ProductosServicios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesactivadoEn",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "DesactivadoPorUsuarioId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "MotivoDesactivacion",
                table: "TBL_ProductosServicios");
        }
    }
}
