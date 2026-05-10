using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Spx.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(entry => entry.Email)
                .HasMaxLength(320);
            user.Property(entry => entry.NormalizedEmail)
                .HasMaxLength(320);
        });

        builder.Entity<Game>(game =>
        {
            game.Property(entry => entry.Name)
                .HasMaxLength(100);
            game.Property(entry => entry.InviteCode)
                .HasMaxLength(6);
            game.Property(entry => entry.CreatedByUserId)
                .HasMaxLength(450);
            game.Property(entry => entry.Status)
                .HasConversion<string>()
                .HasMaxLength(16);

            game.HasIndex(entry => entry.InviteCode)
                .IsUnique();

            game.HasOne(entry => entry.CreatedBy)
                .WithMany()
                .HasForeignKey(entry => entry.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            game.HasMany(entry => entry.Players)
                .WithOne(entry => entry.Game)
                .HasForeignKey(entry => entry.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GamePlayer>(player =>
        {
            player.Property(entry => entry.UserId)
                .HasMaxLength(450);
            player.Property(entry => entry.Name)
                .HasMaxLength(40);
            player.Property(entry => entry.NormalizedName)
                .HasMaxLength(40);

            player.HasIndex(entry => new { entry.GameId, entry.UserId })
                .IsUnique()
                .HasFilter("\"LeftAtUtc\" IS NULL");

            player.HasIndex(entry => new { entry.GameId, entry.NormalizedName })
                .IsUnique()
                .HasFilter("\"LeftAtUtc\" IS NULL");

            player.HasOne(entry => entry.User)
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}