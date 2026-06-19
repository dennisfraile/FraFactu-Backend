using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsReceptorActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Desactivar receptores duplicados por NumeroDocumento (mantener el más reciente activo)
            migrationBuilder.Sql(@"
                UPDATE ""Receptores"" SET ""Activo"" = false
                WHERE ""Id"" NOT IN (
                    SELECT MAX(""Id"") FROM ""Receptores""
                    WHERE ""Activo"" = true
                    GROUP BY ""EmisorId"", ""NumeroDocumento""
                ) AND ""Activo"" = true;
            ");

            // Desactivar receptores duplicados por NRC (mantener el más reciente activo)
            migrationBuilder.Sql(@"
                UPDATE ""Receptores"" SET ""Activo"" = false
                WHERE ""Nrc"" IS NOT NULL AND ""Activo"" = true
                AND ""Id"" NOT IN (
                    SELECT MAX(""Id"") FROM ""Receptores""
                    WHERE ""Activo"" = true AND ""Nrc"" IS NOT NULL
                    GROUP BY ""EmisorId"", ""Nrc""
                );
            ");

            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_NumDoc",
                table: "Receptores");

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_Nrc_Active",
                table: "Receptores",
                columns: new[] { "EmisorId", "Nrc" },
                unique: true,
                filter: "\"Activo\" = true AND \"Nrc\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores",
                columns: new[] { "EmisorId", "NumeroDocumento" },
                unique: true,
                filter: "\"Activo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_Nrc_Active",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores");

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_NumDoc",
                table: "Receptores",
                columns: new[] { "EmisorId", "NumeroDocumento" });
        }
    }
}
