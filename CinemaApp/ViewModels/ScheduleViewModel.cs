using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class ScheduleViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private ObservableCollection<ScheduleDayGroup> _days = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    public ScheduleViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void Load() => _ = LoadAsync();

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var today = DateTime.Today;
            var end = today.AddDays(7);

            var grouped = await Task.Run(() =>
            {
                using var db = new CinemaDbContext();
                var sessions = db.Sessions
                    .Where(s => s.IsActive && s.StartTime >= today && s.StartTime < end)
                    .Include(s => s.Movie)
                    .Include(s => s.Hall)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                return sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new ScheduleDayGroup(g.Key, g.ToList()))
                    .ToList();
            });

            Days = new ObservableCollection<ScheduleDayGroup>(grouped);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectSession(Session session)
    {
        _main.NavigateTo(AppPage.SeatPicker, session);
    }
}

public class ScheduleDayGroup
{
    public DateTime Date { get; }
    public string DayLabel { get; }
    public List<Session> Sessions { get; }

    public ScheduleDayGroup(DateTime date, List<Session> sessions)
    {
        Date = date;
        Sessions = sessions;
        DayLabel = date == DateTime.Today ? "Сегодня"
                 : date == DateTime.Today.AddDays(1) ? "Завтра"
                 : date.ToString("dddd, dd MMMM", new System.Globalization.CultureInfo("ru-RU"));
        DayLabel = char.ToUpper(DayLabel[0]) + DayLabel[1..];
    }
}
