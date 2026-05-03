using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Data;

/// <summary>
/// Creates DB structure on first launch: halls and demo user only.
/// Movies are loaded via TMDB sync (MovieSyncService).
/// Also applies incremental schema patches so old databases stay compatible.
/// </summary>
public static class DatabaseInitializer
{
    public static void Initialize(CinemaDbContext context)
    {
        context.Database.EnsureCreated();

        // ── Incremental schema patches ───────────────────────────────
        ApplySchemaPatches(context);

        // ── Halls ────────────────────────────────────────────────────
        if (!context.Halls.Any())
        {
            context.Halls.AddRange(
                new Hall { Name = "Зал 1 — Dolby Atmos", TotalRows = 12, SeatsPerRow = 14 },
                new Hall { Name = "Зал 2 — IMAX",         TotalRows = 10, SeatsPerRow = 12 },
                new Hall { Name = "Зал 3 — VIP",           TotalRows = 8,  SeatsPerRow = 10 },
                new Hall { Name = "Зал 4 — 4DX",           TotalRows = 9,  SeatsPerRow = 11 }
            );
            context.SaveChanges();
        }

        // ── Demo user ────────────────────────────────────────────────
        if (!context.Users.Any())
        {
            context.Users.Add(new User
            {
                FullName = "Алексей Морозов",
                Email = "user@cinema.ru",
                PasswordHash = HashPassword("password123"),
                Phone = "+7 (900) 123-45-67",
                LoyaltyPoints = 1240,
                LoyaltyLevel = "Синема Клуб",
                IsAdmin = false
            });
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Adds missing columns to existing databases without dropping them.
    /// Each patch is idempotent — safe to run multiple times.
    /// </summary>
    private static void ApplySchemaPatches(CinemaDbContext context)
    {
        // Patch 1: TmdbId column on Movies table
        context.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Movies' AND COLUMN_NAME = 'TmdbId'
            )
            BEGIN
                ALTER TABLE Movies ADD TmdbId INT NOT NULL DEFAULT 0;
            END
            """);

        // Patch 2: OriginalTitle column on Movies table
        context.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Movies' AND COLUMN_NAME = 'OriginalTitle'
            )
            BEGIN
                ALTER TABLE Movies ADD OriginalTitle NVARCHAR(MAX) NOT NULL DEFAULT '';
            END
            """);
    }

    public static string HashPassword(string password) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));

    public static bool CheckPassword(string password, string hash) =>
        hash == HashPassword(password);
}
