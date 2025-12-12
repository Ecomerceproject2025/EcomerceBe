using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponTypeAndShippingMethodCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerUser",
                table: "UserCoupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDiscountAmount",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderAmount",
                table: "Coupons",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Coupons",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShippingMethodCoupons",
                columns: table => new
                {
                    ShippingMethodId = table.Column<int>(type: "int", nullable: false),
                    CouponId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingMethodCoupons", x => new { x.ShippingMethodId, x.CouponId });
                    table.ForeignKey(
                        name: "FK_ShippingMethodCoupons_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "CouponId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShippingMethodCoupons_ShippingMethods_ShippingMethodId",
                        column: x => x.ShippingMethodId,
                        principalTable: "ShippingMethods",
                        principalColumn: "ShippingMethodId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethodCoupons_CouponId",
                table: "ShippingMethodCoupons",
                column: "CouponId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingMethodCoupons");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerUser",
                table: "UserCoupons");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "MinOrderAmount",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Coupons");
        }
    }
}
