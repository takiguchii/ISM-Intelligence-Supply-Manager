using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantProposalsAndPriceRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceRevision",
                table: "dishes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "assistant_proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    AlertId = table.Column<int>(type: "int", nullable: false),
                    Specialist = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ProposedValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SnapshotCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SnapshotTargetMarginPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EvidenceJson = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistant_proposals_system_alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "system_alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "assistant_action_audits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ProposalId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    ValueBefore = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ValueAfter = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    DetailsJson = table.Column<string>(type: "varchar(8000)", maxLength: 8000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_action_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistant_action_audits_assistant_proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "assistant_proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_action_audits_ProposalId",
                table: "assistant_action_audits",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_action_audits_RestaurantId_ProposalId_OccurredAtUtc",
                table: "assistant_action_audits",
                columns: new[] { "RestaurantId", "ProposalId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_proposals_AlertId",
                table: "assistant_proposals",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_proposals_RestaurantId_AlertId",
                table: "assistant_proposals",
                columns: new[] { "RestaurantId", "AlertId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assistant_proposals_RestaurantId_Status_GeneratedAtUtc",
                table: "assistant_proposals",
                columns: new[] { "RestaurantId", "Status", "GeneratedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_action_audits");

            migrationBuilder.DropTable(
                name: "assistant_proposals");

            migrationBuilder.DropColumn(
                name: "PriceRevision",
                table: "dishes");
        }
    }
}
