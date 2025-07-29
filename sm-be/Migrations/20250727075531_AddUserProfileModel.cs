using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sm_be.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bio = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_of_birth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    state = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    profile_picture_url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cover_image_url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occupation = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    website = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    facebook_url = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    twitter_url = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    instagram_url = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    linkedin_url = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_email_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_phone_verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_profile_complete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_email = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_phone_number = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_date_of_birth = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_location = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_social_links = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    allow_messaging = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_online_status = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    preferred_game_mode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preferred_language = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time_zone = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    receive_notifications = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    receive_email_notifications = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    receive_sms_notifications = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    total_games_played = table.Column<int>(type: "int", nullable: false),
                    total_wins = table.Column<int>(type: "int", nullable: false),
                    total_losses = table.Column<int>(type: "int", nullable: false),
                    win_percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    current_streak = table.Column<int>(type: "int", nullable: false),
                    longest_win_streak = table.Column<int>(type: "int", nullable: false),
                    longest_lose_streak = table.Column<int>(type: "int", nullable: false),
                    daily_number_games_played = table.Column<int>(type: "int", nullable: false),
                    daily_number_wins = table.Column<int>(type: "int", nullable: false),
                    digit_games_played = table.Column<int>(type: "int", nullable: false),
                    digit_game_wins = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_active_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_game_played_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    total_achievements = table.Column<int>(type: "int", nullable: false),
                    experience_points = table.Column<int>(type: "int", nullable: false),
                    level = table.Column<int>(type: "int", nullable: false),
                    in_game_money = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    real_money = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_email",
                table: "user_profiles",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profiles");
        }
    }
}
