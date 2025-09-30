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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Player table
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedOnAdd();
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Score).HasDefaultValue(0);

                // FIXED: Added value comparer
                entity.Property(p => p.CurrentAnswerIndexes)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<int>>(v, new JsonSerializerOptions()) ?? new List<int>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });

            // Configure Game table
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(g => g.RoomId);
                entity.Property(g => g.RoomId).HasMaxLength(50);
                entity.Property(g => g.State).HasConversion<string>();

                // FIXED: Added value comparers
                entity.Property(g => g.Players)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<Player>>(v, new JsonSerializerOptions()) ?? new List<Player>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<Player>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                entity.Property(g => g.Questions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<Question>>(v, new JsonSerializerOptions()) ?? new List<Question>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<Question>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });

            // Configure Question table
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Id).ValueGeneratedOnAdd();
                entity.Property(q => q.Text).IsRequired();
                entity.Property(q => q.Difficulty).IsRequired().HasMaxLength(20);
                entity.Property(q => q.Category).IsRequired().HasMaxLength(50);

                // FIXED: Added value comparers
                entity.Property(q => q.Answers)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                entity.Property(q => q.CorrectAnswerIndexes)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<int>>(v, new JsonSerializerOptions()) ?? new List<int>()
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            });
        }
    }
}
