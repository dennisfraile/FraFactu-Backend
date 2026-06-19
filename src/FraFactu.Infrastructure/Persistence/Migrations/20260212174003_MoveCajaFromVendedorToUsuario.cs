using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCajaFromVendedorToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendedores_Cajas_CajaActualId",
                table: "Vendedores");

            migrationBuilder.DropTable(
                name: "HistorialVendedoresCajas");

            migrationBuilder.DropIndex(
                name: "IX_Vendedores_CajaActualId",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "CajaActualId",
                table: "Vendedores");

            migrationBuilder.AddColumn<int>(
                name: "CajaActualId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialUsuariosCajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    CajaId = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioAsignadorId = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialUsuariosCajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialUsuariosCajas_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialUsuariosCajas_Usuarios_UsuarioAsignadorId",
                        column: x => x.UsuarioAsignadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialUsuariosCajas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CajaActualId",
                table: "Usuarios",
                column: "CajaActualId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsuariosCajas_CajaId",
                table: "HistorialUsuariosCajas",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsuariosCajas_UsuarioAsignadorId",
                table: "HistorialUsuariosCajas",
                column: "UsuarioAsignadorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialUsuariosCajas_UsuarioId",
                table: "HistorialUsuariosCajas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Cajas_CajaActualId",
                table: "Usuarios",
                column: "CajaActualId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Cajas_CajaActualId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "HistorialUsuariosCajas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CajaActualId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CajaActualId",
                table: "Usuarios");

            migrationBuilder.AddColumn<int>(
                name: "CajaActualId",
                table: "Vendedores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialVendedoresCajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CajaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioAsignadorId = table.Column<int>(type: "integer", nullable: false),
                    VendedorId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialVendedoresCajas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialVendedoresCajas_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_Vendedores_CajaActualId",
                table: "Vendedores",
                column: "CajaActualId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_CajaId",
                table: "HistorialVendedoresCajas",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_UsuarioAsignadorId",
                table: "HistorialVendedoresCajas",
                column: "UsuarioAsignadorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialVendedoresCajas_VendedorId",
                table: "HistorialVendedoresCajas",
                column: "VendedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendedores_Cajas_CajaActualId",
                table: "Vendedores",
                column: "CajaActualId",
                principalTable: "Cajas",
                principalColumn: "Id");
        }
    }
}
