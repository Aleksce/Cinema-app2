using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Data;

public static class DatabaseInitializer
{
    public static void Initialize(CinemaDbContext context)
    {
        context.Database.EnsureCreated();
        ApplySchemaPatches(context);

        // ── Halls with real addresses ────────────────────────────────
        if (!context.Halls.Any())
        {
            context.Halls.AddRange(
                new Hall
                {
                    Name = "Зал 1 — Dolby Atmos",
                    Address = "ул. Пушкина, 1, ТРЦ «Аврора», 3-й этаж",
                    City = "Москва",
                    TotalRows = 12, SeatsPerRow = 14
                },
                new Hall
                {
                    Name = "Зал 2 — IMAX",
                    Address = "просп. Мира, 45, ТРЦ «Галактика», 4-й этаж",
                    City = "Москва",
                    TotalRows = 10, SeatsPerRow = 12
                },
                new Hall
                {
                    Name = "Зал 3 — VIP",
                    Address = "ул. Ленина, 12, ТРЦ «Планета», 2-й этаж",
                    City = "Москва",
                    TotalRows = 8, SeatsPerRow = 10
                },
                new Hall
                {
                    Name = "Зал 4 — 4DX",
                    Address = "ул. Садовая, 8, ТРЦ «Радуга», 5-й этаж",
                    City = "Москва",
                    TotalRows = 9, SeatsPerRow = 11
                }
            );
            context.SaveChanges();
        }
        else
        {
            // Backfill addresses for existing halls without them
            var halls = context.Halls.ToList();
            string[] addresses = {
                "ул. Пушкина, 1, ТРЦ «Аврора», 3-й этаж",
                "просп. Мира, 45, ТРЦ «Галактика», 4-й этаж",
                "ул. Ленина, 12, ТРЦ «Планета», 2-й этаж",
                "ул. Садовая, 8, ТРЦ «Радуга», 5-й этаж"
            };
            for (int i = 0; i < halls.Count && i < addresses.Length; i++)
            {
                if (string.IsNullOrEmpty(halls[i].Address))
                {
                    halls[i].Address = addresses[i];
                    halls[i].City = "Москва";
                }
            }
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

    private static void ApplySchemaPatches(CinemaDbContext context)
    {
        // Movies table patches
        foreach (var (col, def) in new[]
        {
            ("TmdbId",      "INT NOT NULL DEFAULT 0"),
            ("OriginalTitle","NVARCHAR(MAX) NOT NULL DEFAULT ''"),
            ("TopCastJson",  "NVARCHAR(MAX) NOT NULL DEFAULT '[]'"),
            ("BackdropUrl",  "NVARCHAR(MAX) NOT NULL DEFAULT ''"),
        })
        {
            context.Database.ExecuteSqlRaw($"""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Movies' AND COLUMN_NAME = '{col}'
                )
                ALTER TABLE Movies ADD {col} {def};
                """);
        }

        // Halls table patches
        foreach (var (col, def) in new[]
        {
            ("Address", "NVARCHAR(300) NOT NULL DEFAULT ''"),
            ("City",    "NVARCHAR(100) NOT NULL DEFAULT 'Москва'"),
        })
        {
            context.Database.ExecuteSqlRaw($"""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Halls' AND COLUMN_NAME = '{col}'
                )
                ALTER TABLE Halls ADD {col} {def};
                """);
        }
    }

    public static string HashPassword(string password) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));

    public static bool CheckPassword(string password, string hash) =>
        hash == HashPassword(password);
}
