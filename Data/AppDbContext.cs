using Microsoft.EntityFrameworkCore;
using TournamentApi.Domain;

namespace TournamentApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<Bracket> Brackets => Set<Bracket>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TournamentParticipant - many-to-many
        modelBuilder.Entity<TournamentParticipant>()
            .HasKey(tp => new { tp.TournamentId, tp.UserId });

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId);

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.User)
            .WithMany(u => u.TournamentParticipants)
            .HasForeignKey(tp => tp.UserId);

        // Tournament -> Bracket (1-to-0..1)
        modelBuilder.Entity<Tournament>()
            .HasOne(t => t.Bracket)
            .WithOne(b => b.Tournament)
            .HasForeignKey<Bracket>(b => b.TournamentId);

        // Bracket -> Matches (1-to-many)
        modelBuilder.Entity<Bracket>()
            .HasMany(b => b.Matches)
            .WithOne(m => m.Bracket)
            .HasForeignKey(m => m.BracketId);

        // Match relationships with Users
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany()
            .HasForeignKey(m => m.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany()
            .HasForeignKey(m => m.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
