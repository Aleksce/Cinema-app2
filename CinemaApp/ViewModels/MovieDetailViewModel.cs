using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class MovieDetailViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private Movie? _movie;

    [ObservableProperty]
    private ObservableCollection<Session> _sessions = new();

    [ObservableProperty]
    private ObservableCollection<Session> _todaySessions = new();

    public bool HasNoTodaySessions => !TodaySessions.Any();

    public MovieDetailViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void LoadMovie(Movie movie)
    {
        Movie = movie;
        using var db = new CinemaDbContext();
        var sessions = db.Sessions
            .Where(s => s.MovieId == movie.Id && s.IsActive && s.StartTime >= DateTime.Now)
            .OrderBy(s => s.StartTime)
            .Include(s => s.Hall)
            .ToList();
        Sessions = new ObservableCollection<Session>(sessions);
        var today = sessions.Where(s => s.StartTime.Date == DateTime.Today).ToList();
        TodaySessions = new ObservableCollection<Session>(today);
    }

    [RelayCommand]
    private void SelectSession(Session session)
    {
        _main.NavigateTo(AppPage.SeatPicker, session);
    }

    [RelayCommand]
    private void GoBack()
    {
        _main.NavigateTo(AppPage.Movies);
    }
}
