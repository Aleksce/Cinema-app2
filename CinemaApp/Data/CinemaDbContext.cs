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
            e.Property(m => m.Title).IsRequired().HasMaxLength(300);
            e.Property(m => m.OriginalTitle).HasMaxLength(300);
            e.Property(m => m.Genre).HasMaxLength(200);
            e.Property(m => m.Director).HasMaxLength(200);
            e.Property(m => m.AgeRating).HasMaxLength(10);
            // Index for fast lookup during TMDB sync and genre filtering
            e.HasIndex(m => m.TmdbId).IsUnique().HasDatabaseName("IX_Movies_TmdbId");
            e.HasIndex(m => m.IsActive).HasDatabaseName("IX_Movies_IsActive");
            e.HasIndex(m => m.Genre).HasDatabaseName("IX_Movies_Genre");
        });

        modelBuilder.Entity<Hall>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).IsRequired().HasMaxLength(100);
            e.Property(h => h.Address).HasMaxLength(300);
            e.Property(h => h.City).HasMaxLength(100);
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
            e.Property(s => s.Format).HasMaxLength(10);
            // Indexes for schedule queries (filter by movie, date, active status)
            e.HasIndex(s => s.MovieId).HasDatabaseName("IX_Sessions_MovieId");
            e.HasIndex(s => s.HallId).HasDatabaseName("IX_Sessions_HallId");
            e.HasIndex(s => s.StartTime).HasDatabaseName("IX_Sessions_StartTime");
            e.HasIndex(s => new { s.MovieId, s.StartTime, s.IsActive })
             .HasDatabaseName("IX_Sessions_Movie_Start_Active");
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
            e.Property(s => s.Row).HasMaxLength(5);
            // Critical index: seat picker loads all seats for a session in one query
            e.HasIndex(s => s.SessionId).HasDatabaseName("IX_Seats_SessionId");
            // Composite unique constraint: one seat per row/number per session
            e.HasIndex(s => new { s.SessionId, s.Row, s.Number })
             .IsUnique()
             .HasDatabaseName("UQ_Seats_Session_Row_Number");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.FullName).HasMaxLength(200);
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.LoyaltyLevel).HasMaxLength(50);
            // Unique index for login lookup
            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
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
            e.Property(t => t.SeatRow).HasMaxLength(5);
            e.Property(t => t.QrCode).HasMaxLength(100);
            // Indexes for "My Tickets" page (load by user) and session collision check
            e.HasIndex(t => t.UserId).HasDatabaseName("IX_Tickets_UserId");
            e.HasIndex(t => t.SessionId).HasDatabaseName("IX_Tickets_SessionId");
            // Composite unique: prevent double-booking the same seat
            e.HasIndex(t => new { t.SessionId, t.SeatRow, t.SeatNumber })
             .IsUnique()
             .HasDatabaseName("UQ_Tickets_Session_Seat");
        });
    }
}
