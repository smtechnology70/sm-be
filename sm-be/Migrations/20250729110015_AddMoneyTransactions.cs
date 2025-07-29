using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sm_be.Migrations
{
    /// <inheritdoc />
    public partial class AddMoneyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "money_transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    transaction_direction = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    money_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    transaction_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    game_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    game_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    balance_after = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    in_game_money_after = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    real_money_after = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reference_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoneyTransactions_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_CreatedAt",
                table: "money_transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_GameType",
                table: "money_transactions",
                column: "game_type");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_ReferenceId",
                table: "money_transactions",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_TransactionType",
                table: "money_transactions",
                column: "transaction_type");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_UserId",
                table: "money_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyTransactions_UserId_CreatedAt",
                table: "money_transactions",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "money_transactions");
        }
    }
}
