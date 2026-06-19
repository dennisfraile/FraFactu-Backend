using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSucursalCodigoEstablecimientoIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicar CodigoEstablecimiento existentes: reasignar secuencialmente por Emisor
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"", ""EmisorId"",
                           ROW_NUMBER() OVER (PARTITION BY ""EmisorId"" ORDER BY ""Id"") AS rn
                    FROM ""TBL_Sucursales""
                    WHERE ""Activo"" = true
                )
                UPDATE ""TBL_Sucursales"" s
                SET ""CodigoEstablecimiento"" = LPAD(n.rn::text, 4, '0')
                FROM numbered n
                WHERE s.""Id"" = n.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_Emisor_CodigoEstablecimiento",
                table: "TBL_Sucursales",
                columns: new[] { "EmisorId", "CodigoEstablecimiento" },
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sucursal_Emisor_CodigoEstablecimiento",
                table: "TBL_Sucursales");
        }
    }
}
