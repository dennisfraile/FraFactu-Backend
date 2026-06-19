using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSuscripcionCobrosYGracia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiasGracia",
                table: "Suscripciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProximoCobro",
                table: "Suscripciones",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioProrrateado",
                table: "Suscripciones",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiasGracia",
                table: "Suscripciones");

            migrationBuilder.DropColumn(
                name: "FechaProximoCobro",
                table: "Suscripciones");

            migrationBuilder.DropColumn(
                name: "PrecioProrrateado",
                table: "Suscripciones");
        }
    }
}
