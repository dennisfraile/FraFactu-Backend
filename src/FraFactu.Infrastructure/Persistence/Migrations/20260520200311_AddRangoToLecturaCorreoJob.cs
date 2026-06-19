using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRangoToLecturaCorreoJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAutomatico",
                table: "lectura_correo_jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RangoDesde",
                table: "lectura_correo_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RangoHasta",
                table: "lectura_correo_jobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsAutomatico",
                table: "lectura_correo_jobs");

            migrationBuilder.DropColumn(
                name: "RangoDesde",
                table: "lectura_correo_jobs");

            migrationBuilder.DropColumn(
                name: "RangoHasta",
                table: "lectura_correo_jobs");
        }
    }
}
