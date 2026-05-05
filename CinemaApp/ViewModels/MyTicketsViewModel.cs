using CinemaApp.Data;
using CinemaApp.Models;
using CinemaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class MyTicketsViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty] private ObservableCollection<Ticket> _upcomingTickets = new();
    [ObservableProperty] private ObservableCollection<Ticket> _pastTickets     = new();
    [ObservableProperty] private bool _showUpcoming = true;
    [ObservableProperty] private bool _isUpcomingEmpty;

    public MyTicketsViewModel(MainViewModel main) => _main = main;

    public void Load() => _ = LoadAsync();

    public async Task LoadAsync()
    {
        var user = _main.AccountVm.CurrentUser;
        if (user == null)
        {
            UpcomingTickets = new();
            PastTickets     = new();
            IsUpcomingEmpty = true;
            return;
        }

        IsBusy = true;
        try
        {
            var userId = user.Id;
            var now    = DateTime.Now;

            var (upcoming, past) = await Task.Run(() =>
            {
                using var db = new CinemaDbContext();
                var tickets = db.Tickets
                    .Where(t => t.UserId == userId)
                    .Include(t => t.Session).ThenInclude(s => s.Movie)
                    .Include(t => t.Session).ThenInclude(s => s.Hall)
                    .OrderByDescending(t => t.Session.StartTime)
                    .ToList();

                var up  = tickets.Where(t => t.Session.StartTime >= now
                                          && t.Status == TicketStatus.Active).ToList();
                var p   = tickets.Where(t => t.Session.StartTime < now
                                          || t.Status != TicketStatus.Active).ToList();
                return (up, p);
            });

            UpcomingTickets = new ObservableCollection<Ticket>(upcoming);
            PastTickets     = new ObservableCollection<Ticket>(past);
            IsUpcomingEmpty = !UpcomingTickets.Any();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SwitchToUpcoming() => ShowUpcoming = true;

    [RelayCommand]
    private void SwitchToHistory() => ShowUpcoming = false;

    // ── PDF ticket download ────────────────────────────────────────

    [RelayCommand]
    private void DownloadTicketPdf(Ticket ticket)
    {
        if (ticket == null) return;
        try
        {
            IsBusy = true;
            var path = TicketPdfService.Generate(ticket);
            IsBusy = false;

            var result = System.Windows.MessageBox.Show(
                $"PDF-билет сохранён:\n{path}\n\nОткрыть файл?",
                "Билет готов",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);

            if (result == System.Windows.MessageBoxResult.Yes)
                TicketPdfService.Open(path);
        }
        catch (Exception ex)
        {
            IsBusy = false;
            System.Windows.MessageBox.Show(
                $"Не удалось создать PDF:\n{ex.Message}",
                "Ошибка генерации",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    // ── Cancel ticket ─────────────────────────────────────────────

    [RelayCommand]
    private void CancelTicket(Ticket ticket)
    {
        if (ticket == null) return;

        // Can't cancel a ticket for a session that already started
        if (ticket.Session.StartTime <= DateTime.Now)
        {
            System.Windows.MessageBox.Show(
                "Нельзя отменить билет на прошедший или текущий сеанс.",
                "Отмена невозможна",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var diff = ticket.Session.StartTime - DateTime.Now;
        var timeWarning = diff.TotalHours < 2
            ? $"\n\n⚠ До сеанса меньше {(int)diff.TotalMinutes} минут — возврат может быть невозможен."
            : string.Empty;

        var result = System.Windows.MessageBox.Show(
            $"Отменить билет на «{ticket.MovieTitle}»?\n" +
            $"Место: {ticket.SeatDisplay} · {ticket.SessionTime:dd.MM.yyyy HH:mm}" +
            timeWarning,
            "Подтверждение отмены",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        using var db = new CinemaDbContext();
        var t = db.Tickets.Find(ticket.Id);
        if (t != null)
        {
            t.Status = TicketStatus.Cancelled;

            // Free up the seat so others can book it
            var seat = db.Seats.FirstOrDefault(s =>
                s.SessionId == t.SessionId &&
                s.Row == t.SeatRow &&
                s.Number == t.SeatNumber);
            if (seat != null) seat.Status = SeatStatus.Available;

            db.SaveChanges();
        }
        Load();
    }
}
