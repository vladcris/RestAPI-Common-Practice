using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyBGList.Models;

public class ApplicationDbContext : IdentityDbContext<ApiUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt) : base(opt) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        // customizations go here

        modelBuilder.Entity<BoardGames_Domains>()
            .HasKey(bgd => new { bgd.BoardGameId, bgd.DomainId });

        modelBuilder.Entity<BoardGames_Domains>()
            .HasOne(bgd => bgd.BoardGame)
            .WithMany(y => y.BoardGames_Domains)
            .HasForeignKey(f => f.BoardGameId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardGames_Domains>()
            .HasOne(bgd => bgd.Domain)
            .WithMany(y => y.BoardGames_Domains)
            .HasForeignKey(f => f.DomainId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardGames_Mechanics>()
            .HasKey(bgm => new { bgm.BoardGameId, bgm.MechanicId });

        modelBuilder.Entity<BoardGames_Mechanics>()
            .HasOne(bgd => bgd.BoardGame)
            .WithMany(y => y.BoardGames_Mechanics)
            .HasForeignKey(f => f.BoardGameId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BoardGames_Mechanics>()
            .HasOne(bgd => bgd.Mechanic)
            .WithMany(y => y.BoardGames_Mechanics)
            .HasForeignKey(f => f.MechanicId)
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<BoardGame> BoardGames => Set<BoardGame>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<BoardGames_Domains> BoardGames_Domains => Set<BoardGames_Domains>();
    public DbSet<BoardGames_Mechanics> BoardGames_Mechanics => Set<BoardGames_Mechanics>();

}
