using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "orders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "orders");
        }
    }
}
