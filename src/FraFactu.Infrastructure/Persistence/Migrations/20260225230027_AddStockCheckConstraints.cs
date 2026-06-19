using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CHECK constraint: CantidadDisponible no puede ser negativa
            migrationBuilder.Sql(@"
                ALTER TABLE stock_bodegas
                ADD CONSTRAINT ""CK_StockBodega_CantidadDisponible_NoNegativa""
                CHECK (""CantidadDisponible"" >= 0);
            ");

            // CHECK constraint: CantidadReservada no puede ser negativa
            migrationBuilder.Sql(@"
                ALTER TABLE stock_bodegas
                ADD CONSTRAINT ""CK_StockBodega_CantidadReservada_NoNegativa""
                CHECK (""CantidadReservada"" >= 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE stock_bodegas
                DROP CONSTRAINT IF EXISTS ""CK_StockBodega_CantidadDisponible_NoNegativa"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE stock_bodegas
                DROP CONSTRAINT IF EXISTS ""CK_StockBodega_CantidadReservada_NoNegativa"";
            ");
        }
    }
}
