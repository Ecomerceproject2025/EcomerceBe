using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class seedadmin2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "EmailVerificationExpiry", "EmailVerificationToken", "Name", "PasswordHash", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "Role", "status" },
                values: new object[,]
                {
                    { 5, "", new DateTime(2025, 11, 12, 3, 42, 43, 248, DateTimeKind.Utc).AddTicks(8201), "Hungv5996@gmail.com", null, null, "Hungadmin", "$2a$11$LGWiw6sdNQPKFd3MLYfNHO46xngKImUvZyts.N8eAqIiYGnGuEctq", "", null, null, "Admin", "inActive" },
                    { 6, "", new DateTime(2025, 11, 12, 3, 42, 43, 248, DateTimeKind.Utc).AddTicks(8850), "thaithanhphat323@gmail.com", null, null, "Phatadmin", "$2a$11$LGWiw6sdNQPKFd3MLYfNHO46xngKImUvZyts.N8eAqIiYGnGuEctq", "", null, null, "Admin", "inActive" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
