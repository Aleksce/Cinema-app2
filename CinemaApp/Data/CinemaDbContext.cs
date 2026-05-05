using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Data;

public class CinemaDbContext : DbContext
{
    public DbSet<Movie>   Movies   { get; set; }
    public DbSet<Hall>    Halls    { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Seat>    Seats    { get; set; }
    public DbSet<User>    Users    { get; set; }
    public DbSet<Ticket>  Tickets  { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;Database=CinemaDb;Trusted_Connection=True;MultipleActiveResultSets=true",
            sqlOpts => sqlOpts.CommandTimeout(180));   // 3-minute timeout — enough for any single query
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.Property(m => m.Genre).HasMaxLength(100);
            e.Property(m => m.Director).HasMaxLength(150);
        });

        modelBuilder.Entity<Hall>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Movie)
             .WithMany(m => m.Sessions)
             .HasForeignKey(s => s.MovieId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Hall)
             .WithMany(h => h.Sessions)
             .HasForeignKey(s => s.HallId)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(s => s.BasePrice).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Seat>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Hall)
             .WithMany(h => h.Seats)
             .HasForeignKey(s => s.HallId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(s => s.Session)
             .WithMany(s => s.Seats)
             .HasForeignKey(s => s.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.PriceModifier).HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.User)
             .WithMany(u => u.Tickets)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Session)
             .WithMany(s => s.Tickets)
             .HasForeignKey(t => t.SessionId)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(t => t.Price).HasColumnType("decimal(10,2)");
        });
    }
}
