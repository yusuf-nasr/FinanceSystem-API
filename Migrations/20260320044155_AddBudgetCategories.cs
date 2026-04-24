using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceSystem_Dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BudgetAllocation",
                table: "Transactions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetName",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetCategories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetCategories", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "BudgetEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InputterId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    BudgetName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetEntries_BudgetCategories_BudgetName",
                        column: x => x.BudgetName,
                        principalTable: "BudgetCategories",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetEntries_Users_InputterId",
                        column: x => x.InputterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BudgetName",
                table: "Transactions",
                column: "BudgetName");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetEntries_BudgetName",
                table: "BudgetEntries",
                column: "BudgetName");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetEntries_InputterId",
                table: "BudgetEntries",
                column: "InputterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_BudgetCategories_BudgetName",
                table: "Transactions",
                column: "BudgetName",
                principalTable: "BudgetCategories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_BudgetCategories_BudgetName",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "BudgetEntries");

            migrationBuilder.DropTable(
                name: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BudgetName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetAllocation",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BudgetName",
                table: "Transactions");
        }
    }
}
