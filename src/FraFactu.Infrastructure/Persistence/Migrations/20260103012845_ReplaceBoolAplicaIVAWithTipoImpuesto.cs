using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBoolAplicaIVAWithTipoImpuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AplicaIVA",
                table: "TBL_ProductosServicios");

            migrationBuilder.AddColumn<int>(
                name: "TipoImpuesto",
                table: "TBL_ProductosServicios",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoImpuesto",
                table: "TBL_ProductosServicios");

            migrationBuilder.AddColumn<bool>(
                name: "AplicaIVA",
                table: "TBL_ProductosServicios",
                type: "boolean",
                nullable: true,
                defaultValue: true);
        }
    }
}
