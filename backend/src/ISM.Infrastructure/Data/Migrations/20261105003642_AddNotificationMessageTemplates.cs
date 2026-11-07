using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationMessageTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "notification_deliveries",
                type: "varchar(6000)",
                maxLength: 6000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "notification_deliveries",
                type: "longtext",
                maxLength: 30000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "notification_deliveries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Scenario",
                table: "notification_deliveries",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SourceAlertId",
                table: "notification_deliveries",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE notification_deliveries SET IsTest = 1 WHERE AlertId IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "Scenario",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "SourceAlertId",
                table: "notification_deliveries");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "notification_deliveries",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6000)",
                oldMaxLength: 6000)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
