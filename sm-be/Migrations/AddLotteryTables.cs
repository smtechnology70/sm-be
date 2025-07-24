using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SM_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddLotteryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create daily_numbers table
            migrationBuilder.CreateTable(
                name: "daily_numbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", "IdentityColumn"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    winning_number = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_numbers", x => x.Id);
                    table.CheckConstraint("CK_DailyNumbers_WinningNumber", "winning_number >= 0 AND winning_number <= 99");
                })
                .Annotation("MySql:Charset", "utf8mb4");

            // Create player_entries table
            migrationBuilder.CreateTable(
                name: "player_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", "IdentityColumn"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    daily_number_id = table.Column<int>(type: "int", nullable: false),
                    guessed_number = table.Column<int>(type: "int", nullable: false),
                    entry_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    is_winner = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_entries", x => x.Id);
                    table.CheckConstraint("CK_PlayerEntries_GuessedNumber", "guessed_number >= 0 AND guessed_number <= 99");
                    table.ForeignKey(
                        name: "FK_PlayerEntries_DailyNumbers",
                        column: x => x.daily_number_id,
                        principalTable: "daily_numbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerEntries_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:Charset", "utf8mb4");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_daily_numbers_Date",
                table: "daily_numbers",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEntries_UserId_DailyNumberId",
                table: "player_entries",
                columns: new[] { "user_id", "daily_number_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_entries_daily_number_id",
                table: "player_entries",
                column: "daily_number_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_entries_user_id",
                table: "player_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_entries_entry_time",
                table: "player_entries",
                column: "entry_time");

            migrationBuilder.CreateIndex(
                name: "IX_player_entries_is_winner",
                table: "player_entries",
                column: "is_winner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_entries");

            migrationBuilder.DropTable(
                name: "daily_numbers");
        }
    }
}