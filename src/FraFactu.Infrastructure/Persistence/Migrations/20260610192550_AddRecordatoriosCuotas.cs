using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordatoriosCuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RecordatorioPorVencerEnviado",
                table: "cuotas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RecordatorioVencidaEnviado",
                table: "cuotas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasAntesRecordatorio",
                table: "configuracion_cuotas",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "RecordatoriosHabilitados",
                table: "configuracion_cuotas",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordatorioPorVencerEnviado",
                table: "cuotas");

            migrationBuilder.DropColumn(
                name: "RecordatorioVencidaEnviado",
                table: "cuotas");

            migrationBuilder.DropColumn(
                name: "DiasAntesRecordatorio",
                table: "configuracion_cuotas");

            migrationBuilder.DropColumn(
                name: "RecordatoriosHabilitados",
                table: "configuracion_cuotas");
        }
    }
}
