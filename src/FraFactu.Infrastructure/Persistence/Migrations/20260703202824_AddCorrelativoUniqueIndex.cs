using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelativoUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnioEmision",
                table: "Facturas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill del año en filas existentes ANTES de crear el índice único.
            // AT TIME ZONE 'UTC' recobra el año original (FechaEmision se persiste como
            // SpecifyKind(fecha, Utc)), consistente con AnioEmision = FechaEmision.Year del
            // código y robusto ante el TimeZone de sesión. Un UPDATE puntual no exige que la
            // expresión sea IMMUTABLE (a diferencia de un índice de expresión).
            migrationBuilder.Sql(
                "UPDATE \"Facturas\" SET \"AnioEmision\" = CAST(EXTRACT(YEAR FROM (\"FechaEmision\" AT TIME ZONE 'UTC')) AS integer);");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Emisor_Ambiente_Anio_NumeroControl",
                table: "Facturas",
                columns: new[] { "EmisorId", "Ambiente", "AnioEmision", "NumeroControl" },
                unique: true,
                filter: "\"NumeroControl\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_Emisor_Ambiente_Anio_NumeroControl",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "AnioEmision",
                table: "Facturas");
        }
    }
}
