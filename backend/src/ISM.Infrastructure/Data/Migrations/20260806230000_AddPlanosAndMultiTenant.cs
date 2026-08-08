using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ISM.Infrastructure.Data.Context;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(IsmDbContext))]
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
            // Seeding will be handled by DbSeeder.SeedAsync

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

            migrationBuilder.DropTable(
                name: "planos");
        }
    }
}
