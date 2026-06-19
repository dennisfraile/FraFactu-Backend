using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDteCamposToEmisorReceptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoActividad",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionActividad",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nrc",
                table: "Receptores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIva",
                table: "Facturas",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CodigoActividad",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescripcionActividad",
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
                name: "Nrc",
                table: "emisores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoActividad",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "DescripcionActividad",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "Nrc",
                table: "Receptores");

            migrationBuilder.DropColumn(
                name: "TotalIva",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CodigoActividad",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "DescripcionActividad",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Nrc",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "emisores");

            migrationBuilder.DropColumn(
                name: "TipoEstablecimiento",
                table: "emisores");
        }
    }
}
