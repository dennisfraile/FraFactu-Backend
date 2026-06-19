using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixBodegaIndexManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corrección manual de índice de Bodegas (lo que el usuario solicitó)
            // Se elimina el índice antiguo incorrecto y se crea el nuevo compuesto
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Bodegas_Codigo\";");
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Bodegas_Sucursal_Codigo\" ON bodegas (\"SucursalId\", \"Codigo\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Bodegas_Sucursal_Codigo\";");
            // Recrear el incorrecto si se hace rollback (opcional, pero para simetría)
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Bodegas_Codigo\" ON bodegas (\"Codigo\");");
        }
    }
}
