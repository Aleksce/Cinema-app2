using CinemaApp.Data;
using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Services;

public static class MovieSyncService
{
    private static readonly string[] SessionTimes = { "10:00", "13:00", "16:00", "19:00", "21:30" };
    private static readonly Random Rng = new(Environment.TickCount);

    public static async Task SyncAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Подключение к TMDB...");

        using var tmdb = new TmdbService();
        List<TmdbMovieItem> items;

        try
        {
            var nowPlaying = await tmdb.GetNowPlayingAsync(pages: 3);
            var upcoming   = await tmdb.GetUpcomingAsync(pages: 2);

            items = nowPlaying
                .Concat(upcoming)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .Take(30)
                .ToList();

            progress?.Report($"Получено {items.Count} фильмов из TMDB");
        }
        catch (Exception ex)
        {
            progress?.Report($"Нет соединения с TMDB: {ex.Message}");
            return;
        }

        using var db = new CinemaDbContext();
        var halls = db.Halls.ToList();
        if (!halls.Any()) return;

        int added = 0, updated = 0;

        foreach (var item in items)
        {
            progress?.Report($"Обработка: {item.Title}...");

            TmdbMovieDetails? details = null;
            try { details = await tmdb.GetDetailsAsync(item.Id); }
            catch { }

            var existingMovie = db.Movies.FirstOrDefault(m => m.TmdbId == item.Id);

            if (existingMovie == null)
            {
                var movie = new Movie
                {
                    TmdbId        = item.Id,
                    Title         = item.Title,
                    OriginalTitle = item.OriginalTitle,
                    Description   = details?.Overview ?? item.Overview,
                    Genre         = details != null
                                        ? string.Join(", ", details.Genres.Take(3).Select(g => g.Name))
                                        : "Разное",
                    Director      = details != null ? tmdb.GetDirector(details) ?? "" : "",
                    Cast          = details != null ? tmdb.GetCast(details) : "",
                    DurationMinutes = details?.Runtime ?? 110,
                    Year          = ParseYear(item.ReleaseDate),
                    AgeRating     = tmdb.GetAgeRating(item.Adult),
                    ImdbRating    = Math.Round(item.VoteAverage, 1),
                    ReleaseDate   = ParseDate(item.ReleaseDate),
                    PosterUrl     = item.PosterPath != null ? TmdbService.PosterBase + item.PosterPath : "",
                    TrailerUrl    = details != null ? tmdb.GetTrailerUrl(details) ?? "" : "",
                    IsActive      = true
                };
                db.Movies.Add(movie);
                db.SaveChanges();

                // Generate ONLY Session rows — NO seats yet (lazy seat generation in SeatPicker)
                GenerateSessions(db, movie, halls);
                added++;
            }
            else
            {
                existingMovie.ImdbRating = Math.Round(item.VoteAverage, 1);
                existingMovie.IsActive   = true;
                if (item.PosterPath != null)
                    existingMovie.PosterUrl = TmdbService.PosterBase + item.PosterPath;
                if (details != null && string.IsNullOrEmpty(existingMovie.TrailerUrl))
                    existingMovie.TrailerUrl = tmdb.GetTrailerUrl(details) ?? "";
                updated++;
            }
        }

        db.SaveChanges();

        // Clean up using raw SQL — avoids loading 75 000 seat rows into memory
        CleanupExpiredSessions(db);
        DeactivateMoviesWithNoFutureSessions(db, items.Select(i => i.Id).ToHashSet());

        progress?.Report($"Синхронизация завершена: добавлено {added}, обновлено {updated}");
    }

    // ── Session generation (no seats) ──────────────────────────────
    private static void GenerateSessions(CinemaDbContext db, Movie movie, List<Hall> halls)
    {
        var today = DateTime.Today;

        for (int day = 0; day < 7; day++)
        {
            var date = today.AddDays(day);

            // 2–3 sessions per day
            var timesForDay = SessionTimes
                .OrderBy(_ => Rng.Next())
                .Take(Rng.Next(2, 4))
                .OrderBy(t => t)
                .ToList();

            foreach (var timeStr in timesForDay)
            {
                var parts = timeStr.Split(':');
                var start = date.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
                var hall  = halls[Rng.Next(halls.Count)];
                var fmt   = Rng.Next(3) switch { 0 => "IMAX", 1 => "3D", _ => "2D" };
                var price = 300m + Rng.Next(0, 6) * 50m;

                db.Sessions.Add(new Session
                {
                    Movie     = movie,
                    Hall      = hall,
                    StartTime = start,
                    EndTime   = start.AddMinutes(movie.DurationMinutes + 20),
                    Format    = fmt,
                    BasePrice = price,
                    IsActive  = true
                });
            }
        }

        db.SaveChanges();
    }

    // ── Cleanup via raw SQL — never loads seat rows into memory ─────
    private static void CleanupExpiredSessions(CinemaDbContext db)
    {
        // Step 1: delete seats for expired sessions that have no tickets
        db.Database.ExecuteSqlRaw("""
            DELETE FROM Seats
            WHERE SessionId IN (
                SELECT s.Id FROM Sessions s
                WHERE s.EndTime < DATEADD(HOUR, -24, GETDATE())
                AND NOT EXISTS (SELECT 1 FROM Tickets t WHERE t.SessionId = s.Id)
            )
            """);

        // Step 2: delete those expired sessions themselves
        db.Database.ExecuteSqlRaw("""
            DELETE FROM Sessions
            WHERE EndTime < DATEADD(HOUR, -24, GETDATE())
            AND NOT EXISTS (SELECT 1 FROM Tickets t WHERE t.SessionId = Sessions.Id)
            """);
    }

    // ── Deactivate movies no longer in TMDB that have no future sessions ─
    private static void DeactivateMoviesWithNoFutureSessions(CinemaDbContext db, HashSet<int> activeTmdbIds)
    {
        var now = DateTime.Now;
        var activeMovies = db.Movies.Where(m => m.IsActive).ToList();

        foreach (var movie in activeMovies)
        {
            if (movie.TmdbId > 0 && activeTmdbIds.Contains(movie.TmdbId)) continue;
            var hasFuture = db.Sessions.Any(s => s.MovieId == movie.Id && s.StartTime > now);
            if (!hasFuture) movie.IsActive = false;
        }

        db.SaveChanges();
    }

    // ── Public helper — called by SeatPickerViewModel on demand ─────
    public static void EnsureSeatsExist(CinemaDbContext db, Session session)
    {
        if (db.Seats.Any(s => s.SessionId == session.Id)) return;

        const string rowLetters = "ABCDEFGHIJKLM";
        var seats = new List<Seat>();

        for (int r = 0; r < session.Hall.TotalRows; r++)
        {
            for (int n = 1; n <= session.Hall.SeatsPerRow; n++)
            {
                var type = r >= session.Hall.TotalRows - 2 ? SeatType.VIP
                         : r >= session.Hall.TotalRows - 4 ? SeatType.Sofa
                         : SeatType.Standard;

                seats.Add(new Seat
                {
                    HallId        = session.Hall.Id,
                    SessionId     = session.Id,
                    Row           = rowLetters[r].ToString(),
                    Number        = n,
                    Status        = Rng.Next(10) == 0 ? SeatStatus.Occupied : SeatStatus.Available,
                    Type          = type,
                    PriceModifier = type == SeatType.VIP  ? 1.8m
                                  : type == SeatType.Sofa ? 1.4m
                                  : 1.0m
                });
            }
        }

        db.Seats.AddRange(seats);
        db.SaveChanges();
    }

    private static int ParseYear(string date) =>
        DateTime.TryParse(date, out var d) ? d.Year : DateTime.Today.Year;

    private static DateTime ParseDate(string date) =>
        DateTime.TryParse(date, out var d) ? d : DateTime.Today;
}
