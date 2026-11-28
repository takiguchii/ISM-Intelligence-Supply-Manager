using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaidTabId",
                table: "orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_tabs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    TableReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpenTableReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_tabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_tabs_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PaidTabId",
                table: "orders",
                column: "PaidTabId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_RestaurantId_TableReference_Status",
                table: "orders",
                columns: new[] { "RestaurantId", "TableReference", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_order_tabs_RestaurantId_IdempotencyKey",
                table: "order_tabs",
                columns: new[] { "RestaurantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_tabs_RestaurantId_OpenTableReference",
                table: "order_tabs",
                columns: new[] { "RestaurantId", "OpenTableReference" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_order_tabs_PaidTabId",
                table: "orders",
                column: "PaidTabId",
                principalTable: "order_tabs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_order_tabs_PaidTabId",
                table: "orders");

            migrationBuilder.DropTable(
                name: "order_tabs");

            migrationBuilder.DropIndex(
                name: "IX_orders_PaidTabId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_RestaurantId_TableReference_Status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PaidTabId",
                table: "orders");
        }
    }
}
