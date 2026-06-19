using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredencialesProduccionToEmisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MhClaveApiProd",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MhLlavePrivadaProd",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MhLlavePublicaProd",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MhPassPrivadaProd",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MhUsuarioProd",
                table: "TBL_Emisores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ambiente",
                table: "Facturas",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "00");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EmisorId_Ambiente",
                table: "Facturas",
                columns: new[] { "EmisorId", "Ambiente" });

            // Opción A: Backfill Ambiente desde el payload JWS de JsonFirmado
            // JWS format: header.payload.signature (base64url encoded)
            // Decodifica el payload, parsea como JSON y extrae identificacion->>'ambiente'
            // Fallback: usa el código del ambiente actual del emisor vía cat_ambiente_destino
            migrationBuilder.Sql(@"
                UPDATE ""Facturas"" f
                SET ""Ambiente"" = COALESCE(
                    -- Intentar extraer del JWS payload
                    (
                        SELECT (
                            convert_from(
                                decode(
                                    CASE
                                        WHEN length(payload) % 4 = 2 THEN payload || '=='
                                        WHEN length(payload) % 4 = 3 THEN payload || '='
                                        ELSE payload
                                    END,
                                    'base64'
                                ),
                                'UTF-8'
                            )::jsonb -> 'identificacion' ->> 'ambiente'
                        )
                        FROM (
                            SELECT replace(replace(split_part(f.""JsonFirmado"", '.', 2), '-', '+'), '_', '/') AS payload
                        ) sub
                        WHERE f.""JsonFirmado"" IS NOT NULL
                          AND f.""JsonFirmado"" LIKE '%.%.%'
                          AND length(split_part(f.""JsonFirmado"", '.', 2)) > 0
                    ),
                    -- Fallback: usar ambiente actual del emisor
                    (
                        SELECT cad.""Codigo""
                        FROM ""TBL_Emisores"" e
                        JOIN ""cat_ambiente_destino"" cad ON e.""CatAmbienteDestinoId"" = cad.""Id""
                        WHERE e.""Id"" = f.""EmisorId""
                    ),
                    '00'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_EmisorId_Ambiente",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MhClaveApiProd",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhLlavePrivadaProd",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhLlavePublicaProd",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhPassPrivadaProd",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "MhUsuarioProd",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "Ambiente",
                table: "Facturas");
        }
    }
}
