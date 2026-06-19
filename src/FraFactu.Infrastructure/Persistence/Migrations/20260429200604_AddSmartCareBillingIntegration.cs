using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartCareBillingIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SmartCareClinicId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareCorrelationId",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmartCareVisitId",
                table: "Facturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmartCareWebhookUrl",
                table: "Facturas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_SmartCareCorrelationId",
                table: "Facturas",
                column: "SmartCareCorrelationId",
                unique: true,
                filter: "\"SmartCareCorrelationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_SmartCareCorrelationId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareClinicId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareCorrelationId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareVisitId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SmartCareWebhookUrl",
                table: "Facturas");
        }
    }
}
