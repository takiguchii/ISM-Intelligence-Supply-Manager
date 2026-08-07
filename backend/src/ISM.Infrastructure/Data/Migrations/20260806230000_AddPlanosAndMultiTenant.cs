using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [Migration("20260806230000_AddPlanosAndMultiTenant")]
    public partial class AddPlanosAndMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================
            // 1. TABELA PLANOS
            // ============================
            migrationBuilder.CreateTable(
                name: "planos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxUsuarios = table.Column<int>(type: "int", nullable: false),
                    MaxPratos = table.Column<int>(type: "int", nullable: false),
                    MaxProdutos = table.Column<int>(type: "int", nullable: false),
                    MaxCategorias = table.Column<int>(type: "int", nullable: false),
                    PrecoMensal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_planos_Nome",
                table: "planos",
                column: "Nome",
                unique: true);

            // ============================
            // 2. SEED PLANOS Free / Pro / Enterprise
            // ============================
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            migrationBuilder.InsertData(
                table: "planos",
                columns: new[] { "Nome", "Descricao", "MaxUsuarios", "MaxPratos", "MaxProdutos", "MaxCategorias", "PrecoMensal", "Ativo", "CreatedAtUtc" },
                values: new object[,]
                {
                    { "Free", "Plano trial basico para teste", 2, 10, 20, 3, 0.00m, true, now },
                    { "Pro", "Plano profissional para restaurantes em crescimento", 10, 100, 200, 15, 149.90m, true, now },
                    { "Enterprise", "Plano ilimitado para redes e franquias", 100, 1000, 2000, 100, 499.90m, true, now }
                });

            // ============================
            // 3. RESTAURANTS - NOVAS COLUNAS PLANO
            // ============================
            migrationBuilder.AddColumn<int>(
                name: "PlanoId",
                table: "restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndAtUtc",
                table: "restaurants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlanoAtivo",
                table: "restaurants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            // Atualiza restaurante existente (ID=1) com Plano Pro + Trial 14 dias
            migrationBuilder.Sql(
                @"UPDATE restaurants SET
                    PlanoId = (SELECT Id FROM planos WHERE Nome = 'Pro' LIMIT 1),
                    TrialEndAtUtc = DATE_ADD(CURRENT_TIMESTAMP(6), INTERVAL 14 DAY),
                    PlanoAtivo = 1
                  WHERE Id = 1");

            migrationBuilder.CreateIndex(
                name: "IX_restaurants_PlanoId",
                table: "restaurants",
                column: "PlanoId");

            migrationBuilder.AddForeignKey(
                name: "FK_restaurants_planos_PlanoId",
                table: "restaurants",
                column: "PlanoId",
                principalTable: "planos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ============================
            // 4. PRODUCTS - RESTAURANTID + ISACTIVE
            // ============================
            // Passo 1: adicionar como NULLABLE
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            // Atribui restaurante 1 para produtos existentes
            migrationBuilder.Sql("UPDATE products SET RestaurantId = 1 WHERE RestaurantId IS NULL");

            // Passo 2: transformar em NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Índice único (RestaurantId + Name)
            migrationBuilder.CreateIndex(
                name: "IX_products_RestaurantId_Name",
                table: "products",
                columns: new[] { "RestaurantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_RestaurantId",
                table: "products",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_restaurants_RestaurantId",
                table: "products",
                column: "RestaurantId",
                principalTable: "restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ============================
            // 5. CATEGORIES - RESTAURANTID + ISACTIVE + DISPLAYORDER
            // ============================
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "categories",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE categories SET RestaurantId = 1 WHERE RestaurantId IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "categories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_RestaurantId_Name",
                table: "categories",
                columns: new[] { "RestaurantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_RestaurantId",
                table: "categories",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_restaurants_RestaurantId",
                table: "categories",
                column: "RestaurantId",
                principalTable: "restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ============================
            // 6. DISHES - RESTAURANTID + NOVOS CAMPOS
            // ============================
            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "dishes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "dishes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Highlight",
                table: "dishes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "dishes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE dishes SET RestaurantId = 1 WHERE RestaurantId IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantId",
                table: "dishes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dishes_RestaurantId_Name",
                table: "dishes",
                columns: new[] { "RestaurantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dishes_RestaurantId_CategoryId",
                table: "dishes",
                columns: new[] { "RestaurantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_RestaurantId",
                table: "dishes",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_dishes_restaurants_RestaurantId",
                table: "dishes",
                column: "RestaurantId",
                principalTable: "restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_restaurants_planos_PlanoId",
                table: "restaurants");

            migrationBuilder.DropForeignKey(
                name: "FK_products_restaurants_RestaurantId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_categories_restaurants_RestaurantId",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_dishes_restaurants_RestaurantId",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_restaurants_PlanoId",
                table: "restaurants");

            migrationBuilder.DropIndex(
                name: "IX_products_RestaurantId_Name",
                table: "products");
            migrationBuilder.DropIndex(
                name: "IX_products_RestaurantId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_RestaurantId_Name",
                table: "categories");
            migrationBuilder.DropIndex(
                name: "IX_categories_RestaurantId",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_dishes_RestaurantId_Name",
                table: "dishes");
            migrationBuilder.DropIndex(
                name: "IX_dishes_RestaurantId_CategoryId",
                table: "dishes");
            migrationBuilder.DropIndex(
                name: "IX_dishes_RestaurantId",
                table: "dishes");

            migrationBuilder.DropIndex(
                name: "IX_planos_Nome",
                table: "planos");

            migrationBuilder.DropColumn(
                name: "PlanoId",
                table: "restaurants");
            migrationBuilder.DropColumn(
                name: "TrialEndAtUtc",
                table: "restaurants");
            migrationBuilder.DropColumn(
                name: "PlanoAtivo",
                table: "restaurants");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "products");
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "categories");
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "categories");
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "dishes");
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "dishes");
            migrationBuilder.DropColumn(
                name: "Highlight",
                table: "dishes");
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "dishes");

            migrationBuilder.DropTable(
                name: "planos");
        }
    }
}
