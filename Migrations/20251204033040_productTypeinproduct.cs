using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class productTypeinproduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "FlashStock",
                table: "FlashSaleItems",
                newName: "saleQuantity");

            migrationBuilder.AddColumn<string>(
                name: "productType",
                table: "Products",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "productType",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "saleQuantity",
                table: "FlashSaleItems",
                newName: "FlashStock");

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "decimal(65,30)",
                nullable: true);
        }
    }
}
