using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCCFEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtrosDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaElectronicaId = table.Column<int>(type: "integer", nullable: false),
                    CodDocAsociado = table.Column<int>(type: "integer", nullable: false),
                    DescDocumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetalleDocumento = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtrosDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtrosDocumentos_Facturas_FacturaElectronicaId",
                        column: x => x.FacturaElectronicaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VentaTerceros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FacturaElectronicaId = table.Column<int>(type: "integer", nullable: false),
                    Nit = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaTerceros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaTerceros_Facturas_FacturaElectronicaId",
                        column: x => x.FacturaElectronicaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicosServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OtroDocumentoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nit = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    DocIdentificacion = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    TipoServicio = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicosServicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicosServicios_OtrosDocumentos_OtroDocumentoId",
                        column: x => x.OtroDocumentoId,
                        principalTable: "OtrosDocumentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicosServicios_OtroDocumentoId",
                table: "MedicosServicios",
                column: "OtroDocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtrosDocumentos_FacturaElectronicaId",
                table: "OtrosDocumentos",
                column: "FacturaElectronicaId");

            migrationBuilder.CreateIndex(
                name: "IX_VentaTerceros_FacturaElectronicaId",
                table: "VentaTerceros",
                column: "FacturaElectronicaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicosServicios");

            migrationBuilder.DropTable(
                name: "VentaTerceros");

            migrationBuilder.DropTable(
                name: "OtrosDocumentos");
        }
    }
}
