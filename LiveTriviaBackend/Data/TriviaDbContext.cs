using Microsoft.EntityFrameworkCore;
using live_trivia;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace live_trivia.Data
{
    public class TriviaDbContext : DbContext
    {
        public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options) { }

        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<GamePlayer> GamePlayers { get; set; }
        public DbSet<PlayerAnswer> PlayerAnswers { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var jsonOptions = new JsonSerializerOptions();

            // Configure Player table
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Score).HasDefaultValue(0);
                entity.Property(p => p.CreatedAt).IsRequired();
                entity.Property(p => p.UpdatedAt);

                // Indexes
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.Score);
            });

            // Configure Game table
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(g => g.RoomId);
                entity.Property(g => g.RoomId).HasMaxLength(50);
                entity.Property(g => g.State).HasConversion<string>();
                entity.Property(g => g.CreatedAt).IsRequired();
                entity.Property(g => g.UpdatedAt);
                entity.Property(g => g.StartedAt);
                entity.Property(g => g.EndedAt);

                // Indexes
                entity.HasIndex(g => g.State);
                entity.HasIndex(g => g.CurrentQuestionIndex);
            });

            // Configure Question table
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Id).ValueGeneratedOnAdd();
                entity.Property(q => q.Text).IsRequired();
                entity.Property(q => q.Difficulty).IsRequired().HasMaxLength(20);
                entity.Property(q => q.Category).IsRequired().HasMaxLength(50);
                entity.Property(q => q.CreatedAt).IsRequired();
                entity.Property(q => q.UpdatedAt);

                // Configure JSON serialization for Answers and CorrectAnswerIndexes
                entity.Property(q => q.Answers)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                entity.Property(q => q.CorrectAnswerIndexes)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<int>>(v, jsonOptions) ?? new List<int>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Indexes
                entity.HasIndex(q => q.Category);
                entity.HasIndex(q => q.Difficulty);
                entity.HasIndex(q => new { q.Category, q.Difficulty });
            });

            // Configure GamePlayer table (many-to-many relationship)
            modelBuilder.Entity<GamePlayer>(entity =>
            {
                entity.HasKey(gp => gp.Id);
                entity.Property(gp => gp.Id).ValueGeneratedOnAdd();
                entity.Property(gp => gp.JoinedAt).IsRequired();

                // Composite unique constraint
                entity.HasIndex(gp => new { gp.GameRoomId, gp.PlayerId }).IsUnique();

                // Relationships
                entity.HasOne(gp => gp.Game)
                    .WithMany(g => g.GamePlayers)
                    .HasForeignKey(gp => gp.GameRoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gp => gp.Player)
                    .WithMany(p => p.GamePlayers)
                    .HasForeignKey(gp => gp.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PlayerAnswer table
            modelBuilder.Entity<PlayerAnswer>(entity =>
            {
                entity.HasKey(pa => pa.Id);
                entity.Property(pa => pa.Id).ValueGeneratedOnAdd();
                entity.Property(pa => pa.AnsweredAt).IsRequired();

                // Configure JSON serialization for SelectedAnswerIndexes
                entity.Property(pa => pa.SelectedAnswerIndexes)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonOptions),
                        v => JsonSerializer.Deserialize<List<int>>(v, jsonOptions) ?? new List<int>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                // Index for performance
                entity.HasIndex(pa => new { pa.GameRoomId, pa.PlayerId, pa.QuestionId });

                // Relationships
                entity.HasOne(pa => pa.Game)
                    .WithMany(g => g.PlayerAnswers)
                    .HasForeignKey(pa => pa.GameRoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.Player)
                    .WithMany(p => p.PlayerAnswers)
                    .HasForeignKey(pa => pa.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.Question)
                    .WithMany(q => q.PlayerAnswers)
                    .HasForeignKey(pa => pa.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Game-Question many-to-many relationship
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Questions)
                .WithMany(q => q.Games)
                .UsingEntity<Dictionary<string, object>>(
                    "GameQuestion",
                    j => j.HasOne<Question>().WithMany().HasForeignKey("QuestionId"),
                    j => j.HasOne<Game>().WithMany().HasForeignKey("GameRoomId"),
                    j =>
                    {
                        j.Property("GameRoomId").HasMaxLength(50);
                        j.HasKey("GameRoomId", "QuestionId");
                    });

            // Configure User table
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd();
                entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.CreatedAt).IsRequired();
                entity.Property(u => u.UpdatedAt);

                entity.HasIndex(u => u.Username).IsUnique();

                entity.HasOne(u => u.Player)
                     .WithMany()
                     .HasForeignKey(u => u.PlayerId)
                     .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
