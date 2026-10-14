using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    public partial class AddAgentsStockAndSupplierSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastConsumptionRecalculatedAtUtc",
                table: "products",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumQuantity",
                table: "products",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MovingAverageConsumption",
                table: "products",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderPoint",
                table: "products",
                type: "decimal(10,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    TriggeredByImportId = table.Column<Guid>(type: "char(36)", nullable: true),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_movements_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_movements_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_product_price_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    SupplierRawName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductRawName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    QuantityPurchased = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    NFeAccessKeyOrImportId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PurchasedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_product_price_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_product_price_history_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supplier_product_price_history_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "system_alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceEntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceEntityId = table.Column<int>(type: "int", nullable: true),
                    PayloadSerializedJson = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDismissed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DismissedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DismissedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_system_alerts_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_system_alerts_users_DismissedByUserId",
                        column: x => x.DismissedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ProductId",
                table: "stock_movements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_RestaurantId_CreatedAtUtc",
                table: "stock_movements",
                columns: new[] { "RestaurantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_RestaurantId_ProductId_CreatedAtUtc",
                table: "stock_movements",
                columns: new[] { "RestaurantId", "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_price_history_RestaurantId_ProductRawName_PurchasedAtUtc",
                table: "supplier_product_price_history",
                columns: new[] { "RestaurantId", "ProductRawName", "PurchasedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_price_history_RestaurantId_PurchasedAtUtc",
                table: "supplier_product_price_history",
                columns: new[] { "RestaurantId", "PurchasedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_price_history_SupplierId",
                table: "supplier_product_price_history",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_system_alerts_DismissedByUserId",
                table: "system_alerts",
                column: "DismissedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_system_alerts_RestaurantId_AlertType_GeneratedAtUtc_IsDismissed",
                table: "system_alerts",
                columns: new[] { "RestaurantId", "AlertType", "GeneratedAtUtc", "IsDismissed" });

            migrationBuilder.CreateIndex(
                name: "IX_system_alerts_RestaurantId_GeneratedAtUtc",
                table: "system_alerts",
                columns: new[] { "RestaurantId", "GeneratedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "supplier_product_price_history");

            migrationBuilder.DropTable(
                name: "system_alerts");

            migrationBuilder.DropColumn(
                name: "LastConsumptionRecalculatedAtUtc",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MaximumQuantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MovingAverageConsumption",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "products");
        }
    }
}
