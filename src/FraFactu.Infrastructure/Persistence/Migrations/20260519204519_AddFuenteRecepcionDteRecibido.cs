using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFuenteRecepcionDteRecibido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CargadoPorUsuarioId",
                table: "dtes_recibidos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuenteRecepcion",
                table: "dtes_recibidos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CORREO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargadoPorUsuarioId",
                table: "dtes_recibidos");

            migrationBuilder.DropColumn(
                name: "FuenteRecepcion",
                table: "dtes_recibidos");
        }
    }
}
