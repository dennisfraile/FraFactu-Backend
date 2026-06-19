using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContingenciaResponsableToSucursal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContingenciaNombreResponsable",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContingenciaNumeroDocResponsable",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContingenciaTipoDocResponsable",
                table: "TBL_Sucursales",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContingenciaNombreResponsable",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "ContingenciaNumeroDocResponsable",
                table: "TBL_Sucursales");

            migrationBuilder.DropColumn(
                name: "ContingenciaTipoDocResponsable",
                table: "TBL_Sucursales");
        }
    }
}
