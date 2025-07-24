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

            // Configure table names (optional, for better naming)
            modelBuilder.Entity<DailyNumber>().ToTable("daily_numbers");
            modelBuilder.Entity<PlayerEntry>().ToTable("player_entries");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<DailyDigitGame>().ToTable("daily_digit_games");
            modelBuilder.Entity<PlayerDigitEntry>().ToTable("player_digit_entries");
        }
    }
}