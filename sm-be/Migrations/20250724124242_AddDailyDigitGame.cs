using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sm_be.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyDigitGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_digit_games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    winning_digit = table.Column<int>(type: "int", nullable: true),
                    is_completed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_digit_games", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "player_digit_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    daily_digit_game_id = table.Column<int>(type: "int", nullable: false),
                    selected_digit = table.Column<int>(type: "int", nullable: false),
                    entry_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_winner = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_digit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerDigitEntries_DailyDigitGames",
                        column: x => x.daily_digit_game_id,
                        principalTable: "daily_digit_games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDigitEntries_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_daily_digit_games_Date",
                table: "daily_digit_games",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_digit_entries_daily_digit_game_id",
                table: "player_digit_entries",
                column: "daily_digit_game_id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDigitEntries_UserId_DailyDigitGameId",
                table: "player_digit_entries",
                columns: new[] { "user_id", "daily_digit_game_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_digit_entries");

            migrationBuilder.DropTable(
                name: "daily_digit_games");
        }
    }
}
