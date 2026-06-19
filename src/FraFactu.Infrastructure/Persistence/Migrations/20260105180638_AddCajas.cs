using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCajas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CajaId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cajas_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_CajaId",
                table: "HistorialVendedoresCajas",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CajaId",
                table: "Facturas",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_SucursalId",
                table: "Cajas",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Cajas_CajaId",
                table: "Facturas",
                column: "CajaId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialVendedoresCajas_Cajas_CajaId",
                table: "HistorialVendedoresCajas",
                column: "CajaId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Cajas_CajaId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialVendedoresCajas_Cajas_CajaId",
                table: "HistorialVendedoresCajas");

            migrationBuilder.DropTable(
                name: "Cajas");

            migrationBuilder.DropIndex(
                name: "IX_HistorialVendedoresCajas_CajaId",
                table: "HistorialVendedoresCajas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CajaId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CajaId",
                table: "Facturas");
        }
    }
}
