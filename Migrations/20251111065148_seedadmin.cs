using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class seedadmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "EmailVerificationExpiry", "EmailVerificationToken", "Name", "PasswordHash", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "Role", "status" },
                values: new object[,]
                {
                    { 5, "", new DateTime(2025, 11, 11, 6, 51, 48, 253, DateTimeKind.Utc).AddTicks(3199), "Hungv5996@gmail.com", null, null, "Hungadmin", "$2a$11$v4jvxdCPkTd1Y4p2e9J4w.kC3WjffDuNG0XawzzGchrVbnYi.pg9G", "", null, null, "Admin", "inActive" },
                    { 6, "", new DateTime(2025, 11, 11, 6, 51, 48, 253, DateTimeKind.Utc).AddTicks(3772), "thaithanhphat323@gmail.com", null, null, "Phatadmin", "$2a$11$v4jvxdCPkTd1Y4p2e9J4w.kC3WjffDuNG0XawzzGchrVbnYi.pg9G", "", null, null, "Admin", "inActive" }
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
