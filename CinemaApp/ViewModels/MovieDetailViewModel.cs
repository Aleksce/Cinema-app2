using CinemaApp.Data;
using CinemaApp.Models;
using CinemaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;

namespace CinemaApp.ViewModels;

public class CastMember
{
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl);
}

public partial class MovieDetailViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty] private Movie? _movie;
    [ObservableProperty] private ObservableCollection<Session> _todaySessions = new();
    [ObservableProperty] private ObservableCollection<CastMember> _castMembers = new();
    [ObservableProperty] private bool _hasNoTodaySessions;
    [ObservableProperty] private bool _hasTrailer;
    [ObservableProperty] private bool _isLoadingCast;

    public MovieDetailViewModel(MainViewModel main) => _main = main;

    public void LoadMovie(Movie movie)
    {
        Movie      = movie;
        HasTrailer = !string.IsNullOrEmpty(movie.TrailerUrl);

        // Parse cast from DB
        var cast = DeserializeCast(movie.TopCastJson);
        CastMembers = new ObservableCollection<CastMember>(cast);

        // Load only FUTURE sessions for today
        using var db = new CinemaDbContext();
        var now   = DateTime.Now;
        var today = DateTime.Today;

        var sessions = db.Sessions
            .Where(s => s.MovieId == movie.Id && s.IsActive && s.StartTime >= now)
            .OrderBy(s => s.StartTime)
            .Include(s => s.Hall)
            .ToList();

        var todaySessions = sessions.Where(s => s.StartTime.Date == today).ToList();
        TodaySessions      = new ObservableCollection<Session>(todaySessions);
        HasNoTodaySessions = !todaySessions.Any();

        // If cast / backdrop / trailer missing, fetch from TMDB in background
        bool needsUpdate = CastMembers.Count == 0
                        || string.IsNullOrEmpty(movie.BackdropUrl)
                        || string.IsNullOrEmpty(movie.TrailerUrl);

        if (needsUpdate && movie.TmdbId > 0)
            _ = FetchMissingDataAsync(movie);
    }

    // ── Background TMDB fetch when DB data is incomplete ─────────────
    private async Task FetchMissingDataAsync(Movie movie)
    {
        IsLoadingCast = true;
        try
        {
            using var tmdb = new TmdbService();

            // Fetch details (ru-RU)
            var details = await tmdb.GetDetailsAsync(movie.TmdbId);
            if (details == null) return;

            // Fetch trailer with English fallback
            var trailerUrl = await GetTrailerWithFallbackAsync(tmdb, movie.TmdbId, details);
            var castJson   = tmdb.GetTopCastJson(details);
            var backdropUrl = tmdb.GetBackdropUrl(details) ?? "";

            // Save to DB
            using var db = new CinemaDbContext();
            var dbMovie = await db.Movies.FindAsync(movie.Id);
            if (dbMovie != null)
            {
                if (string.IsNullOrEmpty(dbMovie.TopCastJson) || dbMovie.TopCastJson == "[]")
                    dbMovie.TopCastJson = castJson;
                if (string.IsNullOrEmpty(dbMovie.BackdropUrl))
                    dbMovie.BackdropUrl = backdropUrl;
                if (string.IsNullOrEmpty(dbMovie.TrailerUrl))
                    dbMovie.TrailerUrl = trailerUrl ?? "";
                await db.SaveChangesAsync();
            }

            // Update UI on dispatcher
            var cast = DeserializeCast(castJson);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CastMembers = new ObservableCollection<CastMember>(cast);
                if (!string.IsNullOrEmpty(backdropUrl) && Movie != null)
                    Movie.BackdropUrl = backdropUrl;
                if (!string.IsNullOrEmpty(trailerUrl) && Movie != null)
                {
                    Movie.TrailerUrl = trailerUrl;
                    HasTrailer = true;
                }
            });
        }
        catch { /* fail silently — app still works without extra data */ }
        finally { IsLoadingCast = false; }
    }

    private static async Task<string?> GetTrailerWithFallbackAsync(TmdbService tmdb, int tmdbId, TmdbMovieDetails details)
    {
        // Try the trailer from the details (fetched with ru-RU)
        var url = tmdb.GetTrailerUrl(details);
        if (url != null) return url;

        // Fallback: fetch videos explicitly in en-US
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await http.GetStringAsync(
                $"https://api.themoviedb.org/3/movie/{tmdbId}/videos?api_key=42a2ec31887cd90e5f695ba9c377ad17&language=en-US");
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            foreach (var v in results.EnumerateArray())
            {
                var site = v.GetProperty("site").GetString();
                var type = v.GetProperty("type").GetString();
                if (site == "YouTube" && (type == "Trailer" || type == "Teaser"))
                    return "https://www.youtube.com/watch?v=" + v.GetProperty("key").GetString();
            }
        }
        catch { }

        return null;
    }

    private static List<CastMember> DeserializeCast(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]") return new();
        try
        {
            return JsonSerializer.Deserialize<List<CastMember>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new(); }
    }

    [RelayCommand]
    private void SelectSession(Session session) => _main.NavigateTo(AppPage.SeatPicker, session);

    [RelayCommand]
    private void GoBack() => _main.NavigateTo(AppPage.Movies);

    [RelayCommand]
    private void GoToSchedule() => _main.NavigateTo(AppPage.Schedule);

    [RelayCommand]
    private void WatchTrailer()
    {
        if (string.IsNullOrEmpty(Movie?.TrailerUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = Movie.TrailerUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
