using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índices para mejorar performance en consultas frecuentes
            // NOTA: Algunos índices fueron eliminados porque ya están definidos en las Entity Configurations:
            // - IX_Facturas_FechaEmision (definido en FacturaElectronicaConfiguration línea 62)
            // - IX_Facturas_NumeroControl (definido en FacturaElectronicaConfiguration línea 60)
            // - IX_Lotes_Estado (definido en LoteConfiguration línea 84)
            // - IX_Lotes_FechaCreacion (definido en LoteConfiguration línea 87)
            // - IX_Inventarios_* (tabla Inventarios causa errores, índices removidos temporalmente)

            // Facturas - índice en columna EstadoHacienda (no duplicado)
            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EstadoHacienda",
                table: "Facturas",
                column: "EstadoHacienda");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover solo el índice que se creó en Up()
            migrationBuilder.DropIndex(
                name: "IX_Facturas_EstadoHacienda",
                table: "Facturas");
        }
    }
}
