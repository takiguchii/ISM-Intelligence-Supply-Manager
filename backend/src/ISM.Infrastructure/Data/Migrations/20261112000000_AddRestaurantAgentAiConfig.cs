using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations;

public partial class AddRestaurantAgentAiConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AgentApiKey", "restaurants", type: "varchar(512)", maxLength: 512, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>("AgentApiEndpoint", "restaurants", type: "varchar(512)", maxLength: 512, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>("AgentModel", "restaurants", type: "varchar(128)", maxLength: 128, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("AgentApiKey", "restaurants");
        migrationBuilder.DropColumn("AgentApiEndpoint", "restaurants");
        migrationBuilder.DropColumn("AgentModel", "restaurants");
    }
}
