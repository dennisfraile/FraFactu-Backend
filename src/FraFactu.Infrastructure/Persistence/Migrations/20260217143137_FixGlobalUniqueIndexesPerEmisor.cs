using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixGlobalUniqueIndexesPerEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lotes_CodigoLote",
                table: "lotes");

            migrationBuilder.DropIndex(
                name: "IX_Invalidaciones_CodigoGeneracion",
                table: "invalidaciones");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CodigoGeneracion",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_EventosContingencia_CodigoGeneracion",
                table: "eventos_contingencia");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_CodigoLote_EmisorId",
                table: "lotes",
                columns: new[] { "CodigoLote", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_CodigoGeneracion_EmisorId",
                table: "invalidaciones",
                columns: new[] { "CodigoGeneracion", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CodigoGeneracion_EmisorId",
                table: "Facturas",
                columns: new[] { "CodigoGeneracion", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_CodigoGeneracion_EmisorId",
                table: "eventos_contingencia",
                columns: new[] { "CodigoGeneracion", "EmisorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lotes_CodigoLote_EmisorId",
                table: "lotes");

            migrationBuilder.DropIndex(
                name: "IX_Invalidaciones_CodigoGeneracion_EmisorId",
                table: "invalidaciones");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CodigoGeneracion_EmisorId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_EventosContingencia_CodigoGeneracion_EmisorId",
                table: "eventos_contingencia");

            migrationBuilder.CreateIndex(
                name: "IX_Lotes_CodigoLote",
                table: "lotes",
                column: "CodigoLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invalidaciones_CodigoGeneracion",
                table: "invalidaciones",
                column: "CodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CodigoGeneracion",
                table: "Facturas",
                column: "CodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosContingencia_CodigoGeneracion",
                table: "eventos_contingencia",
                column: "CodigoGeneracion",
                unique: true);
        }
    }
}
