using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaldoDte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saldo_dte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DteCodigoGeneracion = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    TipoDte = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    MontoOriginal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoAcreditado = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    NceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saldo_dte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saldo_dte_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saldo_dte_DteCodigoGeneracion",
                table: "saldo_dte",
                column: "DteCodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saldo_dte_EmisorId",
                table: "saldo_dte",
                column: "EmisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saldo_dte");
        }
    }
}
