using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDuplicateForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_emisores_EmisorId1",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EmisorId1",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_EmisorId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "EmisorId1",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Facturas",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroControl",
                table: "Facturas",
                type: "character varying(31)",
                maxLength: 31,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoGeneracion",
                table: "Facturas",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CodigoGeneracion",
                table: "Facturas",
                column: "CodigoGeneracion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EmisorId_EstadoHacienda",
                table: "Facturas",
                columns: new[] { "EmisorId", "EstadoHacienda" });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_FechaEmision",
                table: "Facturas",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_NumeroControl",
                table: "Facturas",
                column: "NumeroControl");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas",
                column: "ReceptorId",
                principalTable: "Receptores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_CodigoGeneracion",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_EmisorId_EstadoHacienda",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_FechaEmision",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_NumeroControl",
                table: "Facturas");

            migrationBuilder.AddColumn<int>(
                name: "EmisorId1",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Facturas",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroControl",
                table: "Facturas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(31)",
                oldMaxLength: 31);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoGeneracion",
                table: "Facturas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmisorId1",
                table: "Usuarios",
                column: "EmisorId1");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EmisorId",
                table: "Facturas",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Receptores_ReceptorId",
                table: "Facturas",
                column: "ReceptorId",
                principalTable: "Receptores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_TBL_Sucursales_SucursalId",
                table: "Facturas",
                column: "SucursalId",
                principalTable: "TBL_Sucursales",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_emisores_EmisorId",
                table: "Facturas",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_emisores_EmisorId1",
                table: "Usuarios",
                column: "EmisorId1",
                principalTable: "emisores",
                principalColumn: "Id");
        }
    }
}
