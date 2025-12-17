using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcomerceBE.Migrations
{
    /// <inheritdoc />
    public partial class AddModelAITables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductEmbeddings",
                columns: table => new
                {
                    ProductEmbeddingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingVector = table.Column<string>(type: "LONGTEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmbeddingDimension = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModelVersion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEmbeddings", x => x.ProductEmbeddingId);
                    table.ForeignKey(
                        name: "FK_ProductEmbeddings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserBehaviorLogs",
                columns: table => new
                {
                    UserBehaviorLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BehaviorType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViewDuration = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Metadata = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorLogs", x => x.UserBehaviorLogId);
                    table.ForeignKey(
                        name: "FK_UserBehaviorLogs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBehaviorLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductEmbeddings_ProductId",
                table: "ProductEmbeddings",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLogs_ProductId_BehaviorType",
                table: "UserBehaviorLogs",
                columns: new[] { "ProductId", "BehaviorType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLogs_SessionId",
                table: "UserBehaviorLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLogs_UserId_CreatedAt",
                table: "UserBehaviorLogs",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductEmbeddings");

            migrationBuilder.DropTable(
                name: "UserBehaviorLogs");
        }
    }
}
