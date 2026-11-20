using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantProposalExecutionGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "assistant_proposals",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "assistant_proposals",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE assistant_proposals SET ExpiresAtUtc = DATE_ADD(GeneratedAtUtc, INTERVAL 7 DAY), IdempotencyKey = CONCAT('legacy:alert:', AlertId) WHERE IdempotencyKey = ''");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_proposals_RestaurantId_IdempotencyKey",
                table: "assistant_proposals",
                columns: new[] { "RestaurantId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_assistant_proposals_RestaurantId_IdempotencyKey",
                table: "assistant_proposals");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "assistant_proposals");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "assistant_proposals");
        }
    }
}
