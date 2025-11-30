using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class fixproductdbset : Migration
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
                name: "MetaKeywords",
                table: "Products",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "MetaDescription",
                table: "Products",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(160)",
                oldMaxLength: 160)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "MetaKeywords",
                keyValue: null,
                column: "MetaKeywords",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "MetaKeywords",
                table: "Products",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "MetaDescription",
                keyValue: null,
                column: "MetaDescription",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "MetaDescription",
                table: "Products",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(160)",
                oldMaxLength: 160,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "EmailVerificationExpiry", "EmailVerificationToken", "Name", "PasswordHash", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "Role", "status" },
                values: new object[,]
                {
                    { 5, "", new DateTime(2025, 11, 12, 3, 42, 43, 248, DateTimeKind.Utc).AddTicks(8201), "Hungv5996@gmail.com", null, null, "Hungadmin", "$2a$11$LGWiw6sdNQPKFd3MLYfNHO46xngKImUvZyts.N8eAqIiYGnGuEctq", "", null, null, "Admin", "inActive" },
                    { 6, "", new DateTime(2025, 11, 12, 3, 42, 43, 248, DateTimeKind.Utc).AddTicks(8850), "thaithanhphat323@gmail.com", null, null, "Phatadmin", "$2a$11$LGWiw6sdNQPKFd3MLYfNHO46xngKImUvZyts.N8eAqIiYGnGuEctq", "", null, null, "Admin", "inActive" }
                });
        }
    }
}
