using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCodPuntoVentaMHIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicar CodPuntoVentaMH existentes: reasignar secuencialmente por Sucursal
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"", ""SucursalId"",
                           ROW_NUMBER() OVER (PARTITION BY ""SucursalId"" ORDER BY ""Id"") AS rn
                    FROM ""Cajas""
                    WHERE ""Activo"" = true AND ""CodPuntoVentaMH"" != ''
                )
                UPDATE ""Cajas"" c
                SET ""CodPuntoVentaMH"" = LPAD(n.rn::text, 4, '0')
                FROM numbered n
                WHERE c.""Id"" = n.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVentaMH",
                table: "Cajas",
                columns: new[] { "SucursalId", "CodPuntoVentaMH" },
                unique: true,
                filter: "\"Activo\" = true AND \"CodPuntoVentaMH\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVentaMH",
                table: "Cajas");
        }
    }
}
