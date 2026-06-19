using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUbicacionToFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_emisores_EmisorId",
                table: "eventos_contingencia");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_invalidaciones_emisores_EmisorId",
                table: "invalidaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_emisores_EmisorId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_emisores_EmisorId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_emisores_EmisorId",
                table: "TBL_Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_emisores_EmisorId",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_emisores",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "TipoEstablecimiento",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "TipoEstablecimiento",
                table: "emisores");

            migrationBuilder.RenameTable(
                name: "emisores",
                newName: "TBL_Emisores");

            migrationBuilder.AddColumn<int>(
                name: "CatDepartamentoId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatMunicipioId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoEstablecimientoId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "TBL_Emisores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Nrc",
                table: "TBL_Emisores",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NombreComercial",
                table: "TBL_Emisores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nit",
                table: "TBL_Emisores",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "TBL_Emisores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DescripcionActividad",
                table: "TBL_Emisores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoActividad",
                table: "TBL_Emisores",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CatDepartamentoId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatMunicipioId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatTipoEstablecimientoId",
                table: "TBL_Emisores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TBL_Emisores",
                table: "TBL_Emisores",
                column: "Id");

            // UPDATE existing records to valid catalog IDs
            // IMPORTANT: Must be done BEFORE creating FK constraints
            migrationBuilder.Sql(@"
                UPDATE ""TBL_Emisores"" 
                SET ""CatDepartamentoId"" = 6,   -- 6 = San Salvador (código '06')
                    ""CatMunicipioId"" = 14,      -- 14 = San Salvador municipio (código '14')
                    ""CatTipoEstablecimientoId"" = 1  -- 1 = Casa Matriz (código '01')
                WHERE ""CatDepartamentoId"" = 0 
                   OR ""CatMunicipioId"" = 0 
                   OR ""CatTipoEstablecimientoId"" = 0;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""TBL_Sucursales"" 
                SET ""CatDepartamentoId"" = 6,   -- 6 = San Salvador
                    ""CatMunicipioId"" = 14,      -- 14 = San Salvador municipio
                    ""CatTipoEstablecimientoId"" = 2  -- 2 = Sucursal (código '02')
                WHERE ""CatDepartamentoId"" = 0 
                   OR ""CatMunicipioId"" = 0 
                   OR ""CatTipoEstablecimientoId"" = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Sucursales_CatDepartamentoId",
                table: "TBL_Sucursales",
                column: "CatDepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Sucursales_CatMunicipioId",
                table: "TBL_Sucursales",
                column: "CatMunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Sucursales_CatTipoEstablecimientoId",
                table: "TBL_Sucursales",
                column: "CatTipoEstablecimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_CatDepartamentoId",
                table: "TBL_Emisores",
                column: "CatDepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_CatMunicipioId",
                table: "TBL_Emisores",
                column: "CatMunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_CatTipoEstablecimientoId",
                table: "TBL_Emisores",
                column: "CatTipoEstablecimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_Nit",
                table: "TBL_Emisores",
                column: "Nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_Nrc",
                table: "TBL_Emisores",
                column: "Nrc");

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_TBL_Emisores_EmisorId",
                table: "eventos_contingencia",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_TBL_Emisores_EmisorId",
                table: "Facturas",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invalidaciones_TBL_Emisores_EmisorId",
                table: "invalidaciones",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Emisores_cat_departamento_CatDepartamentoId",
                table: "TBL_Emisores",
                column: "CatDepartamentoId",
                principalTable: "cat_departamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Emisores_cat_municipio_CatMunicipioId",
                table: "TBL_Emisores",
                column: "CatMunicipioId",
                principalTable: "cat_municipio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Emisores_cat_tipo_establecimiento_CatTipoEstablecimient~",
                table: "TBL_Emisores",
                column: "CatTipoEstablecimientoId",
                principalTable: "cat_tipo_establecimiento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ProductosServicios_TBL_Emisores_EmisorId",
                table: "TBL_ProductosServicios",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_TBL_Emisores_EmisorId",
                table: "TBL_Sucursales",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_cat_departamento_CatDepartamentoId",
                table: "TBL_Sucursales",
                column: "CatDepartamentoId",
                principalTable: "cat_departamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_cat_municipio_CatMunicipioId",
                table: "TBL_Sucursales",
                column: "CatMunicipioId",
                principalTable: "cat_municipio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_cat_tipo_establecimiento_CatTipoEstablecimie~",
                table: "TBL_Sucursales",
                column: "CatTipoEstablecimientoId",
                principalTable: "cat_tipo_establecimiento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_TBL_Emisores_EmisorId",
                table: "Usuarios",
                column: "EmisorId",
                principalTable: "TBL_Emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_eventos_contingencia_TBL_Emisores_EmisorId",
                table: "eventos_contingencia");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_TBL_Emisores_EmisorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_invalidaciones_TBL_Emisores_EmisorId",
                table: "invalidaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Receptores_TBL_Emisores_EmisorId",
                table: "Receptores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Emisores_cat_departamento_CatDepartamentoId",
                table: "TBL_Emisores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Emisores_cat_municipio_CatMunicipioId",
                table: "TBL_Emisores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Emisores_cat_tipo_establecimiento_CatTipoEstablecimient~",
                table: "TBL_Emisores");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_ProductosServicios_TBL_Emisores_EmisorId",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_TBL_Emisores_EmisorId",
                table: "TBL_Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_cat_departamento_CatDepartamentoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_cat_municipio_CatMunicipioId",
                table: "TBL_Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_TBL_Sucursales_cat_tipo_establecimiento_CatTipoEstablecimie~",
                table: "TBL_Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_TBL_Emisores_EmisorId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Sucursales_CatDepartamentoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Sucursales_CatMunicipioId",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Sucursales_CatTipoEstablecimientoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TBL_Emisores",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_CatDepartamentoId",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_CatMunicipioId",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_CatTipoEstablecimientoId",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_Nit",
                table: "TBL_Emisores");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_Nrc",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CatDepartamentoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CatMunicipioId",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CatTipoEstablecimientoId",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "CatDepartamentoId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CatMunicipioId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "CatTipoEstablecimientoId",
                table: "TBL_Emisores");

            migrationBuilder.RenameTable(
                name: "TBL_Emisores",
                newName: "emisores");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "TBL_Sucursales",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "06");

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "TBL_Sucursales",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "14");

            migrationBuilder.AddColumn<string>(
                name: "TipoEstablecimiento",
                table: "TBL_Sucursales",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "02");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "emisores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Nrc",
                table: "emisores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "NombreComercial",
                table: "emisores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nit",
                table: "emisores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "emisores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DescripcionActividad",
                table: "emisores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoActividad",
                table: "emisores",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(6)",
                oldMaxLength: 6);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoEstablecimiento",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_emisores",
                table: "emisores",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_eventos_contingencia_emisores_EmisorId",
                table: "eventos_contingencia",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invalidaciones_emisores_EmisorId",
                table: "invalidaciones",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receptores_emisores_EmisorId",
                table: "Receptores",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_ProductosServicios_emisores_EmisorId",
                table: "TBL_ProductosServicios",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBL_Sucursales_emisores_EmisorId",
                table: "TBL_Sucursales",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_emisores_EmisorId",
                table: "Usuarios",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
