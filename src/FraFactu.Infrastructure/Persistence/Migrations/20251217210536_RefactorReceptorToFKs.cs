using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorReceptorToFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Receptores");

            migrationBuilder.RenameIndex(
                name: "IX_Receptores_EmisorId",
                table: "Receptores",
                newName: "IX_Receptor_Emisor");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Receptores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Receptores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nrc",
                table: "Receptores",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreRazonSocial",
                table: "Receptores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Receptores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DescripcionActividad",
                table: "Receptores",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorreoElectronico",
                table: "Receptores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoActividad",
                table: "Receptores",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatDepartamentoId",
                table: "Receptores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatMunicipioId",
                table: "Receptores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoDocumentoId",
                table: "Receptores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Migrar datos existentes: setear CatTipoDocumentoId = 1 (NIT) para registros con 0
            migrationBuilder.Sql(@"
                UPDATE ""Receptores""
                SET ""CatTipoDocumentoId"" = 1  -- 1 = NIT por defecto
                WHERE ""CatTipoDocumentoId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Receptor_Emisor_NumDoc",
                table: "Receptores",
                columns: new[] { "EmisorId", "NumeroDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_Receptores_CatDepartamentoId",
                table: "Receptores",
                column: "CatDepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptores_CatMunicipioId",
                table: "Receptores",
                column: "CatMunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Receptores_CatTipoDocumentoId",
                table: "Receptores",
                column: "CatTipoDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_departamento_CatDepartamentoId",
                table: "Receptores",
                column: "CatDepartamentoId",
                principalTable: "cat_departamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_municipio_CatMunicipioId",
                table: "Receptores",
                column: "CatMunicipioId",
                principalTable: "cat_municipio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_cat_tipo_doc_CatTipoDocumentoId",
                table: "Receptores",
                column: "CatTipoDocumentoId",
                principalTable: "cat_tipo_doc",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_departamento_CatDepartamentoId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_municipio_CatMunicipioId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_cat_tipo_doc_CatTipoDocumentoId",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Receptor_Emisor_NumDoc",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Receptores_CatDepartamentoId",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Receptores_CatMunicipioId",
                table: "Receptores");

            migrationBuilder.DropIndex(
                name: "IX_Receptores_CatTipoDocumentoId",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "CatDepartamentoId",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "CatMunicipioId",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "CatTipoDocumentoId",
                table: "Receptores");

            migrationBuilder.RenameIndex(
                name: "IX_Receptor_Emisor",
                table: "Receptores",
                newName: "IX_Receptores_EmisorId");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Receptores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroDocumento",
                table: "Receptores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Nrc",
                table: "Receptores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreRazonSocial",
                table: "Receptores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Receptores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DescripcionActividad",
                table: "Receptores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorreoElectronico",
                table: "Receptores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoActividad",
                table: "Receptores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Receptores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
