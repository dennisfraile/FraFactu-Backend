using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F2_AuthPropio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpira",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Usuarios",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpira",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Usuarios");
        }
    }
}
