using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F3_DevaluacionMobiliario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimaDevaluacion",
                table: "TBL_ProductosServicios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDevaluacionAnual",
                table: "TBL_ProductosServicios",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaUltimaDevaluacion",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "PorcentajeDevaluacionAnual",
                table: "TBL_ProductosServicios");
        }
    }
}
