using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TimeBank.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Administra categorías, usuarios y roles", "Administrador" },
                    { 2, "Ofrece y solicita servicios con su saldo de horas", "Usuario" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            // Administrador inicial para poder entrar la primera vez:
            // correo admin@timebank.com / contraseña Admin2026* (se guarda hasheada).
            // Solo se crea si todavía no existe ese correo.
            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""FirstName"", ""LastName"", ""Email"", ""PasswordHash"", ""Phone"", ""HoursBalance"", ""IsActive"", ""CreatedAt"")
                SELECT 'Admin', 'TimeBank', 'admin@timebank.com',
                       'AQAAAAIAAYagAAAAEOzgMIiQaIPyYXabhD7mW7sR9A8qvOQ5Mjf/9+TzxrHPQTNTs3lVlRharPvf+YbH5A==',
                       NULL, 0, TRUE, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM ""Users"" WHERE LOWER(""Email"") = 'admin@timebank.com');

                INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
                SELECT u.""Id"", 1, NOW() FROM ""Users"" u
                WHERE LOWER(u.""Email"") = 'admin@timebank.com';
            ");

            // Los usuarios que ya existían quedan con el rol Usuario
            migrationBuilder.Sql(@"
                INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
                SELECT u.""Id"", 2, NOW() FROM ""Users"" u
                WHERE NOT EXISTS (SELECT 1 FROM ""UserRoles"" ur WHERE ur.""UserId"" = u.""Id"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
