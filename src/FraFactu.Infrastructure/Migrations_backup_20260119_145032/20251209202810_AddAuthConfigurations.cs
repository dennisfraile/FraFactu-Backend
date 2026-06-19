using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraFactu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roles_permisos_permisos_PermisoId",
                table: "roles_permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_permisos_roles_RolId",
                table: "roles_permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_emisores_EmisorId",
                table: "usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_roles_RolId",
                table: "usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_usuarios",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_EmisorId",
                table: "usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permisos",
                table: "permisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles_permisos",
                table: "roles_permisos");

            migrationBuilder.RenameTable(
                name: "usuarios",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "permisos",
                newName: "Permisos");

            migrationBuilder.RenameTable(
                name: "roles_permisos",
                newName: "RolPermisos");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_RolId",
                table: "Usuarios",
                newName: "IX_Usuarios_RolId");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_Email",
                table: "Usuarios",
                newName: "IX_Usuario_Email");

            migrationBuilder.RenameIndex(
                name: "IX_permisos_Slug",
                table: "Permisos",
                newName: "IX_Permiso_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_roles_permisos_PermisoId",
                table: "RolPermisos",
                newName: "IX_RolPermisos_PermisoId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "Usuarios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<int>(
                name: "EmisorId1",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Permisos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Permisos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permisos",
                table: "Permisos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolPermisos",
                table: "RolPermisos",
                columns: new[] { "RolId", "PermisoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Emisor_Email",
                table: "Usuarios",
                columns: new[] { "EmisorId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmisorId1",
                table: "Usuarios",
                column: "EmisorId1");

            migrationBuilder.CreateIndex(
                name: "IX_Rol_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId",
                principalTable: "Permisos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_emisores_EmisorId",
                table: "Usuarios",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_emisores_EmisorId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_emisores_EmisorId1",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_Emisor_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EmisorId1",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Rol_Nombre",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permisos",
                table: "Permisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolPermisos",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "EmisorId1",
                table: "Usuarios");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "usuarios");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "Permisos",
                newName: "permisos");

            migrationBuilder.RenameTable(
                name: "RolPermisos",
                newName: "roles_permisos");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_RolId",
                table: "usuarios",
                newName: "IX_usuarios_RolId");

            migrationBuilder.RenameIndex(
                name: "IX_Usuario_Email",
                table: "usuarios",
                newName: "IX_usuarios_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Permiso_Slug",
                table: "permisos",
                newName: "IX_permisos_Slug");

            migrationBuilder.RenameIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "roles_permisos",
                newName: "IX_roles_permisos_PermisoId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "usuarios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "NombreCompleto",
                table: "usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "permisos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "permisos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_usuarios",
                table: "usuarios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permisos",
                table: "permisos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles_permisos",
                table: "roles_permisos",
                columns: new[] { "RolId", "PermisoId" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_EmisorId",
                table: "usuarios",
                column: "EmisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_roles_permisos_permisos_PermisoId",
                table: "roles_permisos",
                column: "PermisoId",
                principalTable: "permisos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_permisos_roles_RolId",
                table: "roles_permisos",
                column: "RolId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_emisores_EmisorId",
                table: "usuarios",
                column: "EmisorId",
                principalTable: "emisores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_roles_RolId",
                table: "usuarios",
                column: "RolId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
