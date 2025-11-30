using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class fixfieldimg : Migration
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
                name: "ImageUrl",
                table: "ProductImages",
                type: "LONGTEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(9999)",
                oldMaxLength: 9999)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ProductImages",
                type: "varchar(9999)",
                maxLength: 9999,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "LONGTEXT")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "EmailVerificationExpiry", "EmailVerificationToken", "Name", "PasswordHash", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "Role", "status" },
                values: new object[,]
                {
                    { 5, "", new DateTime(2025, 11, 11, 5, 19, 16, 937, DateTimeKind.Utc).AddTicks(7902), "Hungv5996@gmail.com", null, null, "Hungadmin", "$2a$11$HMfzOfCOcYVDoSxKXtT87OtxObBlzBP5RzJIU/QnWF/QtKbTLkTZa", "", null, null, "Admin", "inActive" },
                    { 6, "", new DateTime(2025, 11, 11, 5, 19, 16, 937, DateTimeKind.Utc).AddTicks(8400), "thaithanhphat323@gmail.com", null, null, "Phatadmin", "$2a$11$HMfzOfCOcYVDoSxKXtT87OtxObBlzBP5RzJIU/QnWF/QtKbTLkTZa", "", null, null, "Admin", "inActive" }
                });
        }
    }
}
