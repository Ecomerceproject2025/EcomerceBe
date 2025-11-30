using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class fixaddressdbset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.AlterColumn<string>(
                name: "Apartment",
                table: "Addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Addresses",
                keyColumn: "Apartment",
                keyValue: null,
                column: "Apartment",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Apartment",
                table: "Addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "EmailVerificationExpiry", "EmailVerificationToken", "Name", "PasswordHash", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "Role", "status" },
                values: new object[,]
                {
                    { 5, "", new DateTime(2025, 11, 12, 4, 30, 1, 395, DateTimeKind.Utc).AddTicks(6316), "Hungv5996@gmail.com", null, null, "Hungadmin", "$2a$11$nBnMJED9oAnHX3ZqRp25UekRXVtymBnlQlhR6nPcEyJ6rQpeHnqqi", "", null, null, "Admin", "inActive" },
                    { 6, "", new DateTime(2025, 11, 12, 4, 30, 1, 395, DateTimeKind.Utc).AddTicks(7364), "thaithanhphat323@gmail.com", null, null, "Phatadmin", "$2a$11$nBnMJED9oAnHX3ZqRp25UekRXVtymBnlQlhR6nPcEyJ6rQpeHnqqi", "", null, null, "Admin", "inActive" }
                });
        }
    }
}
