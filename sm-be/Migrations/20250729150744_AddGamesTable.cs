using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sm_be.Migrations
{
    /// <inheritdoc />
    public partial class AddGamesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    game_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    game_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    player1_id = table.Column<int>(type: "int", nullable: false),
                    player2_id = table.Column<int>(type: "int", nullable: false),
                    winner_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    win_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    game_data = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Player1",
                        column: x => x.player1_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_Player2",
                        column: x => x.player2_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_Winner",
                        column: x => x.winner_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Games_GameId",
                table: "games",
                column: "game_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_GameType",
                table: "games",
                column: "game_type");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Player1Id",
                table: "games",
                column: "player1_id");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Player2Id",
                table: "games",
                column: "player2_id");

            migrationBuilder.CreateIndex(
                name: "IX_Games_StartedAt",
                table: "games",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status",
                table: "games",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Games_WinnerId",
                table: "games",
                column: "winner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
