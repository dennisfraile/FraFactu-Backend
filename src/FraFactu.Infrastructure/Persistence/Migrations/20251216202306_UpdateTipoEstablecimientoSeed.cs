using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTipoEstablecimientoSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 1,
                column: "Valor",
                value: "Casa Matriz");

            migrationBuilder.UpdateData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 2,
                column: "Valor",
                value: "Sucursal");

            migrationBuilder.InsertData(
                table: "cat_tipo_establecimiento",
                columns: new[] { "Id", "Codigo", "Valor" },
                values: new object[,]
                {
                    { 5, "20", "Oficina Administrativa" },
                    { 6, "99", "Otros" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 1,
                column: "Valor",
                value: "Sucursal");

            migrationBuilder.UpdateData(
                table: "cat_tipo_establecimiento",
                keyColumn: "Id",
                keyValue: 2,
                column: "Valor",
                value: "Casa Matriz");
        }
    }
}
