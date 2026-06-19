using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDtesRecibidosAndComprasTrazabilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LecturaCorreoHabilitada",
                table: "TBL_Emisores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaLecturaCorreo",
                table: "TBL_Emisores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoGeneracionDte",
                table: "compras_externas",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroControlDte",
                table: "compras_externas",
                type: "character varying(31)",
                maxLength: 31,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "compras_externas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "MANUAL");

            migrationBuilder.AddColumn<string>(
                name: "SelloRecibidoDte",
                table: "compras_externas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDte",
                table: "compras_externas",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dtes_recibidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    CodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    SelloRecibido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TipoDte = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    NumeroControl = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmisorNit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmisorNombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EmisorNrc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReceptorNit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReceptorNombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    JsonDte = table.Column<string>(type: "text", nullable: false),
                    MontoGravado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoExento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoNoSujeto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IVA = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE"),
                    CompraExternaId = table.Column<int>(type: "integer", nullable: true),
                    MotivoDescarte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailOrigen = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FechaRecepcionEmail = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dtes_recibidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dtes_recibidos_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dtes_recibidos_compras_externas_CompraExternaId",
                        column: x => x.CompraExternaId,
                        principalTable: "compras_externas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DtesRecibidos_CompraExterna",
                table: "dtes_recibidos",
                column: "CompraExternaId");

            migrationBuilder.CreateIndex(
                name: "IX_DtesRecibidos_Emisor_CodigoGeneracion",
                table: "dtes_recibidos",
                columns: new[] { "EmisorId", "CodigoGeneracion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DtesRecibidos_EmisorNit",
                table: "dtes_recibidos",
                column: "EmisorNit");

            migrationBuilder.CreateIndex(
                name: "IX_DtesRecibidos_Estado",
                table: "dtes_recibidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_DtesRecibidos_FechaEmision",
                table: "dtes_recibidos",
                column: "FechaEmision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dtes_recibidos");

            migrationBuilder.DropColumn(
                name: "LecturaCorreoHabilitada",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "UltimaLecturaCorreo",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CodigoGeneracionDte",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "NumeroControlDte",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "SelloRecibidoDte",
                table: "compras_externas");

            migrationBuilder.DropColumn(
                name: "TipoDte",
                table: "compras_externas");
        }
    }
}
