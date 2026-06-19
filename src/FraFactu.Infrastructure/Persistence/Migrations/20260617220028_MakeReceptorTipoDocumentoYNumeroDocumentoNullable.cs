using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeReceptorTipoDocumentoYNumeroDocumentoNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Receptores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "CatTipoDocumentoIdentificacionReceptorId",
                table: "Receptores",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores",
                columns: new[] { "EmisorId", "NumeroDocumento" },
                unique: true,
                filter: "\"Activo\" = true AND \"NumeroDocumento\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Receptores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CatTipoDocumentoIdentificacionReceptorId",
                table: "Receptores",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_NumDoc_Active",
                table: "Receptores",
                columns: new[] { "EmisorId", "NumeroDocumento" },
                unique: true,
                filter: "\"Activo\" = true");
        }
    }
}
