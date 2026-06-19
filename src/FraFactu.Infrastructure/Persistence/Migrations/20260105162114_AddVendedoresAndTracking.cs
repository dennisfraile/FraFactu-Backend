using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVendedoresAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoVendedor",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendedorId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialUsuariosSucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    FechaAcceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialUsuariosSucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialUsuariosSucursales_TBL_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "TBL_Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialUsuariosSucursales_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendedores_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialVendedoresCajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendedorId = table.Column<int>(type: "integer", nullable: false),
                    CajaId = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAsignadorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialVendedoresCajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialVendedoresCajas_Usuarios_UsuarioAsignadorId",
                        column: x => x.UsuarioAsignadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialVendedoresCajas_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_VendedorId",
                table: "Facturas",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsuariosSucursales_SucursalId",
                table: "HistorialUsuariosSucursales",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsuariosSucursales_UsuarioId",
                table: "HistorialUsuariosSucursales",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_UsuarioAsignadorId",
                table: "HistorialVendedoresCajas",
                column: "UsuarioAsignadorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_VendedorId",
                table: "HistorialVendedoresCajas",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_EmisorId",
                table: "Vendedores",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Vendedores_VendedorId",
                table: "Facturas",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Vendedores_VendedorId",
                table: "Facturas");

            migrationBuilder.DropTable(
                name: "HistorialUsuariosSucursales");

            migrationBuilder.DropTable(
                name: "HistorialVendedoresCajas");

            migrationBuilder.DropTable(
                name: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_VendedorId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CodigoVendedor",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "Facturas");
        }
    }
}
