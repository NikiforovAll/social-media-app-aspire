namespace Api.Data;

using Api.Data.Models;
using Microsoft.EntityFrameworkCore;

public class SocialMediaContext(DbContextOptions<SocialMediaContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();
    public DbSet<Follow> Follows => this.Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Follow>()
            .HasKey(f => new { f.FollowerId, f.FollowedId });

        modelBuilder
            .Entity<Follow>()
            .HasOne(f => f.Followed)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder
            .Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
