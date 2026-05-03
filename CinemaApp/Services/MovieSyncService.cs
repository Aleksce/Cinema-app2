using CinemaApp.Data;
using CinemaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaApp.Services;

public static class MovieSyncService
{
    private static readonly string[] SessionTimes = { "10:00", "12:30", "15:00", "17:30", "20:00", "22:30" };
    private static readonly Random Rng = new(Environment.TickCount);

    public static async Task SyncAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Подключение к TMDB...");

        using var tmdb = new TmdbService();
        List<TmdbMovieItem> items;

        try
        {
            var nowPlaying = await tmdb.GetNowPlayingAsync(pages: 3);
            var upcoming = await tmdb.GetUpcomingAsync(pages: 2);

            // Merge, deduplicate by TMDB id
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

            // Fetch full details (credits, videos, runtime)
            TmdbMovieDetails? details = null;
            try { details = await tmdb.GetDetailsAsync(item.Id); }
            catch { /* skip details, use basic info */ }

            var existingMovie = db.Movies.FirstOrDefault(m => m.TmdbId == item.Id);

            if (existingMovie == null)
            {
                // New movie — add to DB
                var movie = new Movie
                {
                    TmdbId = item.Id,
                    Title = item.Title,
                    OriginalTitle = item.OriginalTitle,
                    Description = details?.Overview ?? item.Overview,
                    Genre = details != null
                        ? string.Join(", ", details.Genres.Take(3).Select(g => g.Name))
                        : "Разное",
                    Director = details != null ? tmdb.GetDirector(details) ?? "" : "",
                    Cast = details != null ? tmdb.GetCast(details) : "",
                    DurationMinutes = details?.Runtime ?? 110,
                    Year = ParseYear(item.ReleaseDate),
                    AgeRating = tmdb.GetAgeRating(item.Adult),
                    ImdbRating = Math.Round(item.VoteAverage, 1),
                    ReleaseDate = ParseDate(item.ReleaseDate),
                    PosterUrl = item.PosterPath != null
                        ? TmdbService.PosterBase + item.PosterPath
                        : string.Empty,
                    TrailerUrl = details != null ? tmdb.GetTrailerUrl(details) ?? "" : "",
                    IsActive = true
                };
                db.Movies.Add(movie);
                db.SaveChanges();

                // Generate sessions for next 7 days
                GenerateSessions(db, movie, halls);
                added++;
            }
            else
            {
                // Update rating, poster and mark active
                existingMovie.ImdbRating = Math.Round(item.VoteAverage, 1);
                existingMovie.IsActive = true;
                if (item.PosterPath != null)
                    existingMovie.PosterUrl = TmdbService.PosterBase + item.PosterPath;
                if (details != null && string.IsNullOrEmpty(existingMovie.TrailerUrl))
                    existingMovie.TrailerUrl = tmdb.GetTrailerUrl(details) ?? "";
                updated++;
            }
        }

        db.SaveChanges();

        // ── Clean up ────────────────────────────────────────────────
        CleanupExpiredSessions(db);
        DeactivateMoviesWithNoFutureSessions(db, items.Select(i => i.Id).ToHashSet());

        progress?.Report($"Синхронизация завершена: добавлено {added}, обновлено {updated}");
    }

    private static void GenerateSessions(CinemaDbContext db, Movie movie, List<Hall> halls)
    {
        var today = DateTime.Today;
        var sessions = new List<Session>();

        for (int day = 0; day < 7; day++)
        {
            var date = today.AddDays(day);
            // 2-4 sessions per day
            var timesForDay = SessionTimes
                .OrderBy(_ => Rng.Next())
                .Take(Rng.Next(2, 5))
                .OrderBy(t => t)
                .ToList();

            foreach (var timeStr in timesForDay)
            {
                var parts = timeStr.Split(':');
                var startTime = date.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
                var hall = halls[Rng.Next(halls.Count)];
                var format = Rng.Next(3) switch { 0 => "IMAX", 1 => "3D", _ => "2D" };
                var price = 300m + Rng.Next(0, 6) * 50m;

                sessions.Add(new Session
                {
                    Movie = movie,
                    Hall = hall,
                    StartTime = startTime,
                    EndTime = startTime.AddMinutes(movie.DurationMinutes + 20),
                    Format = format,
                    BasePrice = price,
                    IsActive = true
                });
            }
        }

        // Generate seats for each session
        foreach (var session in sessions)
        {
            db.Sessions.Add(session);
            db.SaveChanges(); // Save to get Session.Id

            GenerateSeats(db, session);
        }

        db.SaveChanges();
    }

    private static void GenerateSeats(CinemaDbContext db, Session session)
    {
        const string rows = "ABCDEFGHIJKLM";
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
                    HallId = session.Hall.Id,
                    SessionId = session.Id,
                    Row = rows[r].ToString(),
                    Number = n,
                    Status = Rng.Next(7) == 0 ? SeatStatus.Occupied : SeatStatus.Available,
                    Type = type,
                    PriceModifier = type == SeatType.VIP ? 1.8m
                                  : type == SeatType.Sofa ? 1.4m
                                  : 1.0m
                });
            }
        }

        db.Seats.AddRange(seats);
    }

    private static void CleanupExpiredSessions(CinemaDbContext db)
    {
        var cutoff = DateTime.Now.AddHours(-24);
        var expired = db.Sessions
            .Where(s => s.EndTime < cutoff)
            .Include(s => s.Seats)
            .ToList();

        foreach (var session in expired)
        {
            // Only delete if no tickets were sold for this session
            var hasTickets = db.Tickets.Any(t => t.SessionId == session.Id);
            if (!hasTickets)
            {
                db.Seats.RemoveRange(session.Seats);
                db.Sessions.Remove(session);
            }
        }

        db.SaveChanges();
    }

    private static void DeactivateMoviesWithNoFutureSessions(CinemaDbContext db, HashSet<int> activeTmdbIds)
    {
        var now = DateTime.Now;
        var activeMovies = db.Movies.Where(m => m.IsActive).ToList();

        foreach (var movie in activeMovies)
        {
            // Skip if still in TMDB now-playing/upcoming list
            if (movie.TmdbId > 0 && activeTmdbIds.Contains(movie.TmdbId)) continue;

            // Check if there are any future sessions
            var hasFutureSessions = db.Sessions.Any(s => s.MovieId == movie.Id && s.StartTime > now);
            if (!hasFutureSessions)
            {
                movie.IsActive = false;
            }
        }

        db.SaveChanges();
    }

    private static int ParseYear(string date)
    {
        if (DateTime.TryParse(date, out var d)) return d.Year;
        return DateTime.Today.Year;
    }

    private static DateTime ParseDate(string date)
    {
        if (DateTime.TryParse(date, out var d)) return d;
        return DateTime.Today;
    }
}
