using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receipt_confirmations",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ContentSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentIdentitySha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_confirmations", x => x.ImportId);
                    table.ForeignKey(
                        name: "FK_receipt_confirmations_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_confirmations_RestaurantId_ContentSha256",
                table: "receipt_confirmations",
                columns: new[] { "RestaurantId", "ContentSha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_confirmations_RestaurantId_DocumentIdentitySha256",
                table: "receipt_confirmations",
                columns: new[] { "RestaurantId", "DocumentIdentitySha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_confirmations_RestaurantId_IdempotencyKey",
                table: "receipt_confirmations",
                columns: new[] { "RestaurantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt_confirmations");
        }
    }
}
