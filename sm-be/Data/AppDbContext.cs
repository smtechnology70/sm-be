using Microsoft.EntityFrameworkCore;
using sm_be.Models.MinimumNumberCount;
using SM_BE.Models;
using SM_BE.Models.Lottery;

namespace SM_BE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DailyNumber> DailyNumbers { get; set; }
        public DbSet<PlayerEntry> PlayerEntries { get; set; }
        
        // New digit game entities
        public DbSet<DailyDigitGame> DailyDigitGames { get; set; }
        public DbSet<PlayerDigitEntry> PlayerDigitEntries { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        
        // Money transaction entity
        public DbSet<MoneyTransaction> MoneyTransactions { get; set; }
        
        // Game records entity
        public DbSet<Game> Games { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            // Configure DailyNumber entity
            modelBuilder.Entity<DailyNumber>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Date).IsUnique();
                entity.Property(e => e.Date).HasColumnType("date");
                entity.Property(e => e.WinningNumber).HasColumnName("winning_number");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Configure PlayerEntry entity
            modelBuilder.Entity<PlayerEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // One user can only have one entry per day (composite unique index)
                entity.HasIndex(e => new { e.UserId, e.DailyNumberId })
                      .IsUnique()
                      .HasDatabaseName("IX_PlayerEntries_UserId_DailyNumberId");
                
                // Column names
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.DailyNumberId).HasColumnName("daily_number_id");
                entity.Property(e => e.GuessedNumber).HasColumnName("guessed_number");
                entity.Property(e => e.EntryTime).HasColumnName("entry_time");
                entity.Property(e => e.IsWinner).HasColumnName("is_winner");
                
                // Configure relationships
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_PlayerEntries_Users");
                
                entity.HasOne(e => e.DailyNumber)
                      .WithMany(d => d.PlayerEntries)
                      .HasForeignKey(e => e.DailyNumberId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_PlayerEntries_DailyNumbers");
            });

            // Configure DailyDigitGame entity
            modelBuilder.Entity<DailyDigitGame>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Date).IsUnique();
                entity.Property(e => e.Date).HasColumnType("date");
                entity.Property(e => e.WinningDigit).HasColumnName("winning_digit");
                entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            });

            // Configure PlayerDigitEntry entity
            modelBuilder.Entity<PlayerDigitEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // One user can only have one entry per day for digit game (composite unique index)
                entity.HasIndex(e => new { e.UserId, e.DailyDigitGameId })
                      .IsUnique()
                      .HasDatabaseName("IX_PlayerDigitEntries_UserId_DailyDigitGameId");
                
                // Column names
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.DailyDigitGameId).HasColumnName("daily_digit_game_id");
                entity.Property(e => e.SelectedDigit).HasColumnName("selected_digit");
                entity.Property(e => e.EntryTime).HasColumnName("entry_time");
                entity.Property(e => e.IsWinner).HasColumnName("is_winner");
                
                // Configure relationships
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_PlayerDigitEntries_Users");
                
                entity.HasOne(e => e.DailyDigitGame)
                      .WithMany(d => d.PlayerDigitEntries)
                      .HasForeignKey(e => e.DailyDigitGameId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_PlayerDigitEntries_DailyDigitGames");
            });

            // Configure UserProfile entity
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.Email);
                
                // Column names and constraints
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).HasColumnName("phone_number");
                entity.Property(e => e.Bio).HasMaxLength(500).HasColumnName("bio");
                entity.Property(e => e.FirstName).HasMaxLength(100).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasMaxLength(100).HasColumnName("last_name");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.Gender).HasMaxLength(10).HasColumnName("gender");
                entity.Property(e => e.Country).HasMaxLength(100).HasColumnName("country");
                entity.Property(e => e.State).HasMaxLength(100).HasColumnName("state");
                entity.Property(e => e.City).HasMaxLength(100).HasColumnName("city");
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(255).HasColumnName("profile_picture_url");
                entity.Property(e => e.CoverImageUrl).HasMaxLength(255).HasColumnName("cover_image_url");
                entity.Property(e => e.Occupation).HasMaxLength(100).HasColumnName("occupation");
                entity.Property(e => e.Website).HasMaxLength(255).HasColumnName("website");
                entity.Property(e => e.FacebookUrl).HasMaxLength(100).HasColumnName("facebook_url");
                entity.Property(e => e.TwitterUrl).HasMaxLength(100).HasColumnName("twitter_url");
                entity.Property(e => e.InstagramUrl).HasMaxLength(100).HasColumnName("instagram_url");
                entity.Property(e => e.LinkedInUrl).HasMaxLength(100).HasColumnName("linkedin_url");
                
                // Boolean and numeric fields
                entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
                entity.Property(e => e.IsPhoneVerified).HasColumnName("is_phone_verified");
                entity.Property(e => e.IsProfileComplete).HasColumnName("is_profile_complete");
                entity.Property(e => e.ShowEmail).HasColumnName("show_email");
                entity.Property(e => e.ShowPhoneNumber).HasColumnName("show_phone_number");
                entity.Property(e => e.ShowDateOfBirth).HasColumnName("show_date_of_birth");
                entity.Property(e => e.ShowLocation).HasColumnName("show_location");
                entity.Property(e => e.ShowSocialLinks).HasColumnName("show_social_links");
                entity.Property(e => e.AllowMessaging).HasColumnName("allow_messaging");
                entity.Property(e => e.ShowOnlineStatus).HasColumnName("show_online_status");
                entity.Property(e => e.ReceiveNotifications).HasColumnName("receive_notifications");
                entity.Property(e => e.ReceiveEmailNotifications).HasColumnName("receive_email_notifications");
                entity.Property(e => e.ReceiveSmsNotifications).HasColumnName("receive_sms_notifications");
                
                // Gaming fields
                entity.Property(e => e.PreferredGameMode).HasMaxLength(50).HasColumnName("preferred_game_mode");
                entity.Property(e => e.PreferredLanguage).HasMaxLength(20).HasColumnName("preferred_language");
                entity.Property(e => e.TimeZone).HasMaxLength(10).HasColumnName("time_zone");
                entity.Property(e => e.TotalGamesPlayed).HasColumnName("total_games_played");
                entity.Property(e => e.TotalWins).HasColumnName("total_wins");
                entity.Property(e => e.TotalLosses).HasColumnName("total_losses");
                entity.Property(e => e.WinPercentage).HasColumnName("win_percentage").HasColumnType("decimal(5,2)");
                entity.Property(e => e.CurrentStreak).HasColumnName("current_streak");
                entity.Property(e => e.LongestWinStreak).HasColumnName("longest_win_streak");
                entity.Property(e => e.LongestLoseStreak).HasColumnName("longest_lose_streak");
                entity.Property(e => e.DailyNumberGamesPlayed).HasColumnName("daily_number_games_played");
                entity.Property(e => e.DailyNumberWins).HasColumnName("daily_number_wins");
                entity.Property(e => e.DigitGamesPlayed).HasColumnName("digit_games_played");
                entity.Property(e => e.DigitGameWins).HasColumnName("digit_game_wins");
                entity.Property(e => e.TotalAchievements).HasColumnName("total_achievements");
                entity.Property(e => e.ExperiencePoints).HasColumnName("experience_points");
                entity.Property(e => e.Level).HasColumnName("level");
                
                // Timestamp fields
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.LastActiveAt).HasColumnName("last_active_at");
                entity.Property(e => e.LastGamePlayedAt).HasColumnName("last_game_played_at");
                
                // Configure relationship
                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<UserProfile>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_UserProfiles_Users");

                //Money User Will Have
                entity.Property(e => e.RealMoney).HasColumnName("real_money").HasColumnType("decimal(18,2)");
                entity.Property(e => e.InGameMoney).HasColumnName("in_game_money").HasColumnType("decimal(18,2)");
            });

            // Configure MoneyTransaction entity
            modelBuilder.Entity<MoneyTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Indexes for common queries
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_MoneyTransactions_UserId");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_MoneyTransactions_CreatedAt");
                entity.HasIndex(e => e.TransactionType).HasDatabaseName("IX_MoneyTransactions_TransactionType");
                entity.HasIndex(e => e.GameType).HasDatabaseName("IX_MoneyTransactions_GameType");
                entity.HasIndex(e => e.ReferenceId).HasDatabaseName("IX_MoneyTransactions_ReferenceId");
                entity.HasIndex(e => new { e.UserId, e.CreatedAt }).HasDatabaseName("IX_MoneyTransactions_UserId_CreatedAt");
                
                // Column names and constraints
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
                entity.Property(e => e.TransactionDirection).HasMaxLength(20).HasColumnName("transaction_direction");
                entity.Property(e => e.MoneyType).HasMaxLength(20).HasColumnName("money_type");
                entity.Property(e => e.TransactionType).HasMaxLength(100).HasColumnName("transaction_type");
                entity.Property(e => e.Description).HasMaxLength(255).HasColumnName("description");
                entity.Property(e => e.GameType).HasMaxLength(50).HasColumnName("game_type");
                entity.Property(e => e.GameId).HasMaxLength(100).HasColumnName("game_id");
                entity.Property(e => e.BalanceAfter).HasColumnName("balance_after").HasColumnType("decimal(18,2)");
                entity.Property(e => e.InGameMoneyAfter).HasColumnName("in_game_money_after").HasColumnType("decimal(18,2)");
                entity.Property(e => e.RealMoneyAfter).HasColumnName("real_money_after").HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.ReferenceId).HasMaxLength(50).HasColumnName("reference_id");
                
                // Configure relationship
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_MoneyTransactions_Users");
            });

            // Configure Game entity
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Indexes for common queries
                entity.HasIndex(e => e.GameId).IsUnique().HasDatabaseName("IX_Games_GameId");
                entity.HasIndex(e => e.GameType).HasDatabaseName("IX_Games_GameType");
                entity.HasIndex(e => e.Player1Id).HasDatabaseName("IX_Games_Player1Id");
                entity.HasIndex(e => e.Player2Id).HasDatabaseName("IX_Games_Player2Id");
                entity.HasIndex(e => e.WinnerId).HasDatabaseName("IX_Games_WinnerId");
                entity.HasIndex(e => e.StartedAt).HasDatabaseName("IX_Games_StartedAt");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Games_Status");
                
                // Column names and constraints
                entity.Property(e => e.GameId).HasMaxLength(100).HasColumnName("game_id");
                entity.Property(e => e.GameType).HasMaxLength(50).HasColumnName("game_type");
                entity.Property(e => e.Player1Id).HasColumnName("player1_id");
                entity.Property(e => e.Player2Id).HasColumnName("player2_id");
                entity.Property(e => e.WinnerId).HasColumnName("winner_id");
                entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
                entity.Property(e => e.EntryFee).HasColumnName("entry_fee").HasColumnType("decimal(18,2)");
                entity.Property(e => e.WinAmount).HasColumnName("win_amount").HasColumnType("decimal(18,2)");
                entity.Property(e => e.StartedAt).HasColumnName("started_at");
                entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
                entity.Property(e => e.GameData).HasMaxLength(500).HasColumnName("game_data");
                
                // Configure relationships
                entity.HasOne(e => e.Player1)
                      .WithMany()
                      .HasForeignKey(e => e.Player1Id)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Games_Player1");
                
                entity.HasOne(e => e.Player2)
                      .WithMany()
                      .HasForeignKey(e => e.Player2Id)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Games_Player2");
                
                entity.HasOne(e => e.Winner)
                      .WithMany()
                      .HasForeignKey(e => e.WinnerId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Games_Winner");
            });

            // Configure table names (optional, for better naming)
            modelBuilder.Entity<DailyNumber>().ToTable("daily_numbers");
            modelBuilder.Entity<PlayerEntry>().ToTable("player_entries");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<DailyDigitGame>().ToTable("daily_digit_games");
            modelBuilder.Entity<PlayerDigitEntry>().ToTable("player_digit_entries");
            modelBuilder.Entity<UserProfile>().ToTable("user_profiles");
            modelBuilder.Entity<MoneyTransaction>().ToTable("money_transactions");
            modelBuilder.Entity<Game>().ToTable("games");
        }
    }
}