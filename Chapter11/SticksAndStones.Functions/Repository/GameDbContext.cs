using Microsoft.EntityFrameworkCore;
using SticksAndStones.Models;

namespace SticksAndStones.Repository;

internal class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<Player> Players { get; set; }
    public DbSet<Game> Games { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Game);

        modelBuilder.Entity<Game>()
            .HasKey(g => g.Id);
        modelBuilder.Entity<Game>()
            .HasOne(g => g.PlayerOne);
        modelBuilder.Entity<Game>()
            .HasOne(e => e.PlayerTwo);
        modelBuilder.Entity<Game>()
            .HasOne(e => e.NextPlayer);
    }
}