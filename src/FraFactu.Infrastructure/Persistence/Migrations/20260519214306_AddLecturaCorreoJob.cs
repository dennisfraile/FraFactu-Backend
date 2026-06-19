using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturaCorreoJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lectura_correo_jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmisorId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ENCOLADO"),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorreosProcesados = table.Column<int>(type: "integer", nullable: false),
                    DtesEncontrados = table.Column<int>(type: "integer", nullable: false),
                    DtesNuevos = table.Column<int>(type: "integer", nullable: false),
                    DtesDuplicados = table.Column<int>(type: "integer", nullable: false),
                    Errores = table.Column<int>(type: "integer", nullable: false),
                    MensajeError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IniciadoPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lectura_correo_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lectura_correo_jobs_TBL_Emisores_EmisorId",
                        column: x => x.EmisorId,
                        principalTable: "TBL_Emisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LecturaCorreoJobs_Emisor_Estado",
                table: "lectura_correo_jobs",
                columns: new[] { "EmisorId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_LecturaCorreoJobs_Emisor_FechaCreacion",
                table: "lectura_correo_jobs",
                columns: new[] { "EmisorId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_LecturaCorreoJobs_Estado",
                table: "lectura_correo_jobs",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lectura_correo_jobs");
        }
    }
}
