namespace Postgres;

using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Postgres.Models;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => this.Set<User>();
    public DbSet<Follow> Follows => this.Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DefineUser(modelBuilder.Entity<User>());
        DefineFollow(modelBuilder.Entity<Follow>());

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var faker = new Faker<User>()
            .UseSeed(1001)
            .RuleFor(u => u.UserId, f => f.IndexFaker + 1)
            .RuleFor(u => u.Email, f => f.Person.Email)
            .RuleFor(u => u.Name, f => f.Person.FullName);

        const int numberOfUsers = 1000;
        modelBuilder.Entity<User>().HasData(faker.Generate(numberOfUsers));

        var followFaker = new Faker<Follow>()
            .UseSeed(1001)
            .RuleFor(f => f.FollowerId, f => f.Random.Number(1, numberOfUsers))
            .RuleFor(f => f.FollowedId, f => f.Random.Number(1, numberOfUsers));

        modelBuilder
            .Entity<Follow>()
            .HasData(followFaker.Generate(numberOfUsers));
    }

    private static void DefineUser(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).UseHiLo("user_id_hilo").IsRequired();

        builder.Property(u => u.Name).HasMaxLength(128);
    }

    private static void DefineFollow(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => new { f.FollowerId, f.FollowedId });

        builder
            .HasOne(f => f.Followed)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
