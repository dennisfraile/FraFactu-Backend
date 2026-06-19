using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHubLinkConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HubSucursalId",
                table: "TBL_Sucursales",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_HubSucursalId",
                table: "TBL_Sucursales",
                column: "HubSucursalId",
                unique: true,
                filter: "\"HubSucursalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TBL_Emisores_HubId",
                table: "TBL_Emisores",
                column: "HubId",
                unique: true,
                filter: "\"HubId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sucursal_HubSucursalId",
                table: "TBL_Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_TBL_Emisores_HubId",
                table: "TBL_Emisores");

            migrationBuilder.DropColumn(
                name: "HubSucursalId",
                table: "TBL_Sucursales");
        }
    }
}
