using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUbicacionToSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoEstablecimiento",
                table: "TBL_Sucursales",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "TipoEstablecimiento",
                table: "TBL_Sucursales");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoPuntoVentaMH",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoPuntoVenta",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoEstablecimiento",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4,
                oldNullable: true);
        }
    }
}
