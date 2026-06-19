using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUniquenessValidationFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usar IF EXISTS porque los nombres de índice en la BD real pueden diferir
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Vendedores_Emisor_Codigo"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Producto_Emisor_Codigo"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Marcas_Nombre_EmisorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Categorias_Codigo_EmisorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_cat_tipo_gasto_EmisorId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CatTipoGasto_Codigo"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Cajas_Sucursal_CodPuntoVenta"";");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor_Codigo",
                table: "TBL_ProductosServicios",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas",
                columns: new[] { "Nombre", "EmisorId" },
                unique: true,
                filter: "\"Activa\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Codigo_EmisorId",
                table: "categorias",
                columns: new[] { "Codigo", "EmisorId" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Emisor_Codigo",
                table: "cat_tipo_gasto",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true,
                filter: "\"Activo\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVenta",
                table: "Cajas",
                columns: new[] { "SucursalId", "CodPuntoVenta" },
                unique: true,
                filter: "\"Activo\" = true AND \"CodPuntoVenta\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores");

            migrationBuilder.DropIndex(
                name: "IX_Producto_Emisor_Codigo",
                table: "TBL_ProductosServicios");

            migrationBuilder.DropIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Codigo_EmisorId",
                table: "categorias");

            migrationBuilder.DropIndex(
                name: "IX_CatTipoGasto_Emisor_Codigo",
                table: "cat_tipo_gasto");

            migrationBuilder.DropIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVenta",
                table: "Cajas");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_Emisor_Codigo",
                table: "Vendedores",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Emisor_Codigo",
                table: "TBL_ProductosServicios",
                columns: new[] { "EmisorId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Nombre_EmisorId",
                table: "marcas",
                columns: new[] { "Nombre", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Codigo_EmisorId",
                table: "categorias",
                columns: new[] { "Codigo", "EmisorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_tipo_gasto_EmisorId",
                table: "cat_tipo_gasto",
                column: "EmisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CatTipoGasto_Codigo",
                table: "cat_tipo_gasto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Sucursal_CodPuntoVenta",
                table: "Cajas",
                columns: new[] { "SucursalId", "CodPuntoVenta" },
                unique: true,
                filter: "\"CodPuntoVenta\" != ''");
        }
    }
}
