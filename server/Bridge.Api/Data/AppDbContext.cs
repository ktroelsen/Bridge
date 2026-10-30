using Bridge.Api.Players;
using Microsoft.EntityFrameworkCore;

namespace Bridge.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();

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
    }
}
