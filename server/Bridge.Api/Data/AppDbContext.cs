using Bridge.Api.Players;
using Microsoft.EntityFrameworkCore;

namespace Bridge.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<Challenge> Challenges => Set<Challenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(PlayerEndpoints.MaxNameLength);
            e.HasIndex(p => p.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<LessonProgress>(e =>
        {
            e.Property(p => p.LessonId).HasMaxLength(PlayerEndpoints.MaxLessonIdLength);
            e.HasOne(p => p.Player).WithMany().HasForeignKey(p => p.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.PlayerId, p.LessonId }).IsUnique();
        });

        modelBuilder.Entity<GameRecord>(e =>
        {
            e.Property(g => g.Mode).HasMaxLength(8);
            e.Property(g => g.Level).HasMaxLength(8);
            e.Property(g => g.Contract).HasMaxLength(4);
            e.Property(g => g.Declarer).HasMaxLength(8);
            e.HasOne(g => g.Player).WithMany().HasForeignKey(g => g.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(g => new { g.PlayerId, g.FinishedAt });
            e.HasOne<Challenge>().WithMany().HasForeignKey(g => g.ChallengeId).OnDelete(DeleteBehavior.Cascade);
            // One result per player and deal in a challenge.
            e.HasIndex(g => new { g.ChallengeId, g.PlayerId, g.Number }).IsUnique().HasFilter("ChallengeId IS NOT NULL");
        });

        modelBuilder.Entity<Challenge>(e =>
        {
            e.Property(c => c.Code).HasMaxLength(8);
            e.Property(c => c.Mode).HasMaxLength(8);
            e.Property(c => c.Level).HasMaxLength(8);
            e.HasIndex(c => c.Code).IsUnique();
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
