using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDishTargetMarginPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoImportApiEndpoint",
                table: "restaurants",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoImportApiKey",
                table: "restaurants",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhotoImportModel",
                table: "restaurants",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TargetMarginPercent",
                table: "dishes",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tmp_authorizations_CreatedByUserId",
                table: "tmp_authorizations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tmp_authorizations_RevokedByUserId",
                table: "tmp_authorizations",
                column: "RevokedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tmp_authorizations_CreatedByUserId",
                table: "tmp_authorizations");

            migrationBuilder.DropIndex(
                name: "IX_tmp_authorizations_RevokedByUserId",
                table: "tmp_authorizations");

            migrationBuilder.DropColumn(
                name: "PhotoImportApiEndpoint",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "PhotoImportApiKey",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "PhotoImportModel",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "TargetMarginPercent",
                table: "dishes");
        }
    }
}
