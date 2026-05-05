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

    // Observable wrappers for fields that can update asynchronously
    // (Movie is a plain EF entity — no INotifyPropertyChanged)
    [ObservableProperty] private string _backdropUrl = string.Empty;
    [ObservableProperty] private string _trailerUrl  = string.Empty;
    [ObservableProperty] private bool   _hasTrailer;
    [ObservableProperty] private bool   _isLoadingCast;

    [ObservableProperty] private ObservableCollection<Session>    _todaySessions = new();
    [ObservableProperty] private ObservableCollection<CastMember> _castMembers   = new();
    [ObservableProperty] private bool _hasNoTodaySessions;

    public MovieDetailViewModel(MainViewModel main) => _main = main;

    public void LoadMovie(Movie movie)
    {
        Movie       = movie;
        BackdropUrl = movie.BackdropUrl;
        TrailerUrl  = movie.TrailerUrl;
        HasTrailer  = !string.IsNullOrEmpty(movie.TrailerUrl);

        // Parse cast already stored in DB
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

        var todaySessions  = sessions.Where(s => s.StartTime.Date == today).ToList();
        TodaySessions      = new ObservableCollection<Session>(todaySessions);
        HasNoTodaySessions = !todaySessions.Any();

        // If any rich data is missing → fetch from TMDB in background
        bool needsUpdate = CastMembers.Count == 0
                        || string.IsNullOrEmpty(BackdropUrl)
                        || string.IsNullOrEmpty(TrailerUrl);

        if (needsUpdate && movie.TmdbId > 0)
            _ = FetchMissingDataAsync(movie);
        else if (needsUpdate && movie.TmdbId == 0)
            // Try to find by title search if TmdbId not set
            _ = FetchByTitleAsync(movie);
    }

    // ── Fetch by TMDB ID (normal path) ──────────────────────────────
    private async Task FetchMissingDataAsync(Movie movie)
    {
        IsLoadingCast = true;
        try
        {
            using var tmdb = new TmdbService();
            var details = await tmdb.GetDetailsAsync(movie.TmdbId);
            if (details == null) return;

            await ApplyTmdbData(movie, tmdb, details);
        }
        catch { }
        finally { IsLoadingCast = false; }
    }

    // ── Fetch by title search when TmdbId = 0 ───────────────────────
    private async Task FetchByTitleAsync(Movie movie)
    {
        IsLoadingCast = true;
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var q    = Uri.EscapeDataString(movie.OriginalTitle.Length > 2 ? movie.OriginalTitle : movie.Title);
            var json = await http.GetStringAsync(
                $"https://api.themoviedb.org/3/search/movie?api_key=42a2ec31887cd90e5f695ba9c377ad17&language=ru-RU&query={q}&year={movie.Year}");

            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            if (!results.EnumerateArray().Any()) return;

            int tmdbId = results.EnumerateArray().First().GetProperty("id").GetInt32();
            if (tmdbId == 0) return;

            // Save TmdbId
            using var db = new CinemaDbContext();
            var dbMovie = await db.Movies.FindAsync(movie.Id);
            if (dbMovie != null) { dbMovie.TmdbId = tmdbId; await db.SaveChangesAsync(); }
            movie.TmdbId = tmdbId;

            using var tmdb = new TmdbService();
            var details = await tmdb.GetDetailsAsync(tmdbId);
            if (details == null) return;

            await ApplyTmdbData(movie, tmdb, details);
        }
        catch { }
        finally { IsLoadingCast = false; }
    }

    // ── Apply TMDB details to DB + update observable properties ─────
    private async Task ApplyTmdbData(Movie movie, TmdbService tmdb, TmdbMovieDetails details)
    {
        var castJson   = tmdb.GetTopCastJson(details);
        var backdrop   = tmdb.GetBackdropUrl(details) ?? string.Empty;
        var trailer    = await GetTrailerWithFallbackAsync(tmdb, details);

        // Persist to DB
        using var db = new CinemaDbContext();
        var dbMovie = await db.Movies.FindAsync(movie.Id);
        if (dbMovie != null)
        {
            if (string.IsNullOrEmpty(dbMovie.TopCastJson) || dbMovie.TopCastJson == "[]")
                dbMovie.TopCastJson = castJson;
            if (string.IsNullOrEmpty(dbMovie.BackdropUrl) && !string.IsNullOrEmpty(backdrop))
                dbMovie.BackdropUrl = backdrop;
            if (string.IsNullOrEmpty(dbMovie.TrailerUrl) && !string.IsNullOrEmpty(trailer))
                dbMovie.TrailerUrl = trailer;
            await db.SaveChangesAsync();
        }

        var cast = DeserializeCast(castJson);

        // Update observable properties on UI thread — bindings will refresh automatically
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CastMembers = new ObservableCollection<CastMember>(cast);

            if (!string.IsNullOrEmpty(backdrop))
                BackdropUrl = backdrop;

            if (!string.IsNullOrEmpty(trailer))
            {
                TrailerUrl = trailer;
                HasTrailer = true;
            }
        });
    }

    // ── Get trailer, fall back to en-US if ru-RU returns nothing ────
    private static async Task<string> GetTrailerWithFallbackAsync(TmdbService tmdb, TmdbMovieDetails details)
    {
        var url = tmdb.GetTrailerUrl(details);
        if (url != null) return url;

        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await http.GetStringAsync(
                $"https://api.themoviedb.org/3/movie/{details.Id}/videos" +
                $"?api_key=42a2ec31887cd90e5f695ba9c377ad17&language=en-US");

            using var doc = JsonDocument.Parse(json);
            foreach (var v in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                var site = v.GetProperty("site").GetString();
                var type = v.GetProperty("type").GetString();
                if (site == "YouTube" && (type == "Trailer" || type == "Teaser"))
                    return "https://www.youtube.com/watch?v=" + v.GetProperty("key").GetString();
            }
        }
        catch { }

        return string.Empty;
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
        if (string.IsNullOrEmpty(TrailerUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = TrailerUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
