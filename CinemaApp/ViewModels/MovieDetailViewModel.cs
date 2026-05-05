using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

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

    public MovieDetailViewModel(MainViewModel main) => _main = main;

    public void LoadMovie(Movie movie)
    {
        Movie = movie;
        HasTrailer = !string.IsNullOrEmpty(movie.TrailerUrl);

        // Parse cast JSON
        try
        {
            var cast = JsonSerializer.Deserialize<List<CastMember>>(
                movie.TopCastJson ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            CastMembers = new ObservableCollection<CastMember>(cast);
        }
        catch { CastMembers = new(); }

        // Load only FUTURE sessions for today (StartTime >= DateTime.Now)
        using var db = new CinemaDbContext();
        var now   = DateTime.Now;
        var today = DateTime.Today;
        var tmr   = today.AddDays(1);

        var sessions = db.Sessions
            .Where(s => s.MovieId == movie.Id && s.IsActive && s.StartTime >= now)
            .OrderBy(s => s.StartTime)
            .Include(s => s.Hall)
            .ToList();

        var todaySessions = sessions.Where(s => s.StartTime.Date == today).ToList();
        TodaySessions       = new ObservableCollection<Session>(todaySessions);
        HasNoTodaySessions  = !todaySessions.Any();
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
        catch { /* ignore if browser not found */ }
    }
}
