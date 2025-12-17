using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeColorToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "OrderItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "OrderItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "OrderItems");
        }
    }
}
