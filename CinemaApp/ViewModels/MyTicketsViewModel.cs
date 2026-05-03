using CinemaApp.Data;
using CinemaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class MyTicketsViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private ObservableCollection<Ticket> _upcomingTickets = new();

    [ObservableProperty]
    private ObservableCollection<Ticket> _pastTickets = new();

    [ObservableProperty]
    private bool _showUpcoming = true;

    [ObservableProperty]
    private bool _isUpcomingEmpty;

    public MyTicketsViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void Load()
    {
        var user = _main.AccountVm.CurrentUser;
        if (user == null)
        {
            UpcomingTickets = new();
            PastTickets = new();
            return;
        }

        using var db = new CinemaDbContext();
        var tickets = db.Tickets
            .Where(t => t.UserId == user.Id)
            .Include(t => t.Session).ThenInclude(s => s.Movie)
            .Include(t => t.Session).ThenInclude(s => s.Hall)
            .OrderByDescending(t => t.Session.StartTime)
            .ToList();

        UpcomingTickets = new ObservableCollection<Ticket>(
            tickets.Where(t => t.Session.StartTime >= DateTime.Now && t.Status == TicketStatus.Active));
        PastTickets = new ObservableCollection<Ticket>(
            tickets.Where(t => t.Session.StartTime < DateTime.Now || t.Status != TicketStatus.Active));
        IsUpcomingEmpty = !UpcomingTickets.Any();
    }

    [RelayCommand]
    private void SwitchToUpcoming() => ShowUpcoming = true;

    [RelayCommand]
    private void SwitchToHistory() => ShowUpcoming = false;

    [RelayCommand]
    private void ShowTicketQr(Ticket ticket)
    {
        System.Windows.MessageBox.Show(
            $"Ваш QR-код:\n\n{ticket.QrCode}\n\nФильм: {ticket.MovieTitle}\nМесто: {ticket.SeatDisplay}\nСеанс: {ticket.SessionTime:dd.MM.yyyy HH:mm}",
            "Билет", System.Windows.MessageBoxButton.OK);
    }

    [RelayCommand]
    private void CancelTicket(Ticket ticket)
    {
        var result = System.Windows.MessageBox.Show(
            $"Отменить билет на «{ticket.MovieTitle}»?",
            "Подтверждение", System.Windows.MessageBoxButton.YesNo);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        using var db = new CinemaDbContext();
        var t = db.Tickets.Find(ticket.Id);
        if (t != null)
        {
            t.Status = TicketStatus.Cancelled;
            db.SaveChanges();
        }
        Load();
    }
}
