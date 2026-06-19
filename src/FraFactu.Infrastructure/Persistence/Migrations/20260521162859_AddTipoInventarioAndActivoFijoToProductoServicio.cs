using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoInventarioAndActivoFijoToProductoServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AniosVidaUtil",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAdquisicion",
                table: "TBL_ProductosServicios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoInventario",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorActual",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorResidual",
                table: "TBL_ProductosServicios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AniosVidaUtil",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "FechaAdquisicion",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "TipoInventario",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "ValorActual",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropColumn(
                name: "ValorResidual",
                table: "TBL_ProductosServicios");
        }
    }
}
