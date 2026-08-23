using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dishing_ingredients_dishes_DishId",
                table: "dishing_ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_dishing_ingredients_products_ProductId",
                table: "dishing_ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_restaurants_planos_PlanoId",
                table: "restaurants");

            // Rename plans table and columns
            migrationBuilder.RenameTable(
                name: "planos",
                newName: "plans");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "plans",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Descricao",
                table: "plans",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "PrecoMensal",
                table: "plans",
                newName: "MonthlyPrice");

            migrationBuilder.RenameColumn(
                name: "MaxUsuarios",
                table: "plans",
                newName: "MaxUsers");

            migrationBuilder.RenameColumn(
                name: "MaxPratos",
                table: "plans",
                newName: "MaxDishes");

            migrationBuilder.RenameColumn(
                name: "MaxProdutos",
                table: "plans",
                newName: "MaxProducts");

            migrationBuilder.RenameColumn(
                name: "MaxCategorias",
                table: "plans",
                newName: "MaxCategories");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "plans",
                newName: "IsActive");

            // Rename suppliers table (no columns to rename as they are already in English)
            migrationBuilder.RenameTable(
                name: "fornecedores",
                newName: "suppliers");

            // Rename dishing_ingredients table to dish_ingredients
            migrationBuilder.DropPrimaryKey(
                name: "PK_dishing_ingredients",
                table: "dishing_ingredients");

            migrationBuilder.RenameTable(
                name: "dishing_ingredients",
                newName: "dish_ingredients");

            migrationBuilder.RenameColumn(
                name: "PlanoId",
                table: "restaurants",
                newName: "PlanId");

            migrationBuilder.RenameColumn(
                name: "PlanoAtivo",
                table: "restaurants",
                newName: "IsPlanActive");

            migrationBuilder.RenameIndex(
                name: "IX_restaurants_PlanoId",
                table: "restaurants",
                newName: "IX_restaurants_PlanId");

            migrationBuilder.RenameIndex(
                name: "IX_dishing_ingredients_ProductId",
                table: "dish_ingredients",
                newName: "IX_dish_ingredients_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dish_ingredients",
                table: "dish_ingredients",
                columns: new[] { "DishId", "ProductId" });

            migrationBuilder.RenameIndex(
                name: "IX_planos_Nome",
                table: "plans",
                newName: "IX_plans_Name");

            migrationBuilder.RenameIndex(
                name: "IX_fornecedores_RestaurantId_Name",
                table: "suppliers",
                newName: "IX_suppliers_RestaurantId_Name");

            migrationBuilder.AddForeignKey(
                name: "FK_dish_ingredients_dishes_DishId",
                table: "dish_ingredients",
                column: "DishId",
                principalTable: "dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dish_ingredients_products_ProductId",
                table: "dish_ingredients",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_restaurants_plans_PlanId",
                table: "restaurants",
                column: "PlanId",
                principalTable: "plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dish_ingredients_dishes_DishId",
                table: "dish_ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_dish_ingredients_products_ProductId",
                table: "dish_ingredients");

            migrationBuilder.DropForeignKey(
                name: "FK_restaurants_plans_PlanId",
                table: "restaurants");

            // Rename columns back in plans
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "plans",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "plans",
                newName: "Descricao");

            migrationBuilder.RenameColumn(
                name: "MonthlyPrice",
                table: "plans",
                newName: "PrecoMensal");

            migrationBuilder.RenameColumn(
                name: "MaxUsers",
                table: "plans",
                newName: "MaxUsuarios");

            migrationBuilder.RenameColumn(
                name: "MaxDishes",
                table: "plans",
                newName: "MaxPratos");

            migrationBuilder.RenameColumn(
                name: "MaxProducts",
                table: "plans",
                newName: "MaxProdutos");

            migrationBuilder.RenameColumn(
                name: "MaxCategories",
                table: "plans",
                newName: "MaxCategorias");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "plans",
                newName: "Ativo");

            // Rename tables back
            migrationBuilder.RenameTable(
                name: "plans",
                newName: "planos");

            migrationBuilder.RenameTable(
                name: "suppliers",
                newName: "fornecedores");

            // Rename dish_ingredients back to dishing_ingredients
            migrationBuilder.DropPrimaryKey(
                name: "PK_dish_ingredients",
                table: "dish_ingredients");

            migrationBuilder.RenameTable(
                name: "dish_ingredients",
                newName: "dishing_ingredients");

            migrationBuilder.RenameColumn(
                name: "PlanId",
                table: "restaurants",
                newName: "PlanoId");

            migrationBuilder.RenameColumn(
                name: "IsPlanActive",
                table: "restaurants",
                newName: "PlanoAtivo");

            migrationBuilder.RenameIndex(
                name: "IX_restaurants_PlanId",
                table: "restaurants",
                newName: "IX_restaurants_PlanoId");

            migrationBuilder.RenameIndex(
                name: "IX_dish_ingredients_ProductId",
                table: "dishing_ingredients",
                newName: "IX_dishing_ingredients_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dishing_ingredients",
                table: "dishing_ingredients",
                columns: new[] { "DishId", "ProductId" });

            migrationBuilder.RenameIndex(
                name: "IX_plans_Name",
                table: "planos",
                newName: "IX_planos_Nome");

            migrationBuilder.RenameIndex(
                name: "IX_suppliers_RestaurantId_Name",
                table: "fornecedores",
                newName: "IX_fornecedores_RestaurantId_Name");

            migrationBuilder.AddForeignKey(
                name: "FK_dishing_ingredients_dishes_DishId",
                table: "dishing_ingredients",
                column: "DishId",
                principalTable: "dishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dishing_ingredients_products_ProductId",
                table: "dishing_ingredients",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_restaurants_planos_PlanoId",
                table: "restaurants",
                column: "PlanoId",
                principalTable: "planos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
