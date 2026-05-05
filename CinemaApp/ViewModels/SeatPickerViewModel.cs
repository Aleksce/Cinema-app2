using CinemaApp.Data;
using CinemaApp.Models;
using CinemaApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public partial class SeatViewModel : ObservableObject
{
    public Seat Seat { get; }
    private readonly SeatPickerViewModel _parent;

    [ObservableProperty]
    private bool _isSelected;

    public string Row     => Seat.Row;
    public int    Number  => Seat.Number;
    public SeatStatus Status => Seat.Status;
    public SeatType   Type   => Seat.Type;

    public SeatViewModel(Seat seat, SeatPickerViewModel parent)
    {
        Seat    = seat;
        _parent = parent;
    }

    public void Toggle()
    {
        if (Status == SeatStatus.Occupied) return;
        IsSelected = !IsSelected;
        _parent.UpdateSelection();
    }
}

public partial class SeatRowViewModel : ObservableObject
{
    public string RowLabel { get; }
    public ObservableCollection<SeatViewModel> Seats { get; }

    public SeatRowViewModel(string label, IEnumerable<SeatViewModel> seats)
    {
        RowLabel = label;
        Seats    = new ObservableCollection<SeatViewModel>(seats);
    }
}

public partial class SeatPickerViewModel : BaseViewModel
{
    private readonly MainViewModel _main;

    [ObservableProperty] private Session? _session;
    [ObservableProperty] private ObservableCollection<SeatRowViewModel> _rows = new();
    [ObservableProperty] private string  _selectionSummary = "Выберите места";
    [ObservableProperty] private decimal _totalPrice;
    [ObservableProperty] private int     _selectedCount;
    [ObservableProperty] private bool    _hasSelection;

    public SeatPickerViewModel(MainViewModel main) => _main = main;

    // Called by MainViewModel.NavigateTo — fire-and-forget, non-blocking
    public void LoadSession(Session session) => _ = LoadSessionAsync(session);

    public async Task LoadSessionAsync(Session session)
    {
        Session = session;
        IsBusy  = true;

        try
        {
            var rows = await Task.Run(() =>
            {
                using var db = new CinemaDbContext();

                // Load hall if not already eager-loaded (needed for EnsureSeatsExist)
                var fullSession = db.Sessions
                    .Include(s => s.Hall)
                    .Include(s => s.Movie)
                    .FirstOrDefault(s => s.Id == session.Id);

                if (fullSession == null) return new List<Seat>();

                // Lazy seat generation — only for THIS session, only once
                MovieSyncService.EnsureSeatsExist(db, fullSession);

                return db.Seats
                    .Where(s => s.SessionId == session.Id)
                    .OrderBy(s => s.Row)
                    .ThenBy(s => s.Number)
                    .ToList();
            });

            var vms     = rows.Select(s => new SeatViewModel(s, this)).ToList();
            var grouped = vms.GroupBy(s => s.Row)
                             .Select(g => new SeatRowViewModel(g.Key, g))
                             .ToList();
            Rows = new ObservableCollection<SeatRowViewModel>(grouped);
            UpdateSelection();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleSeat(SeatViewModel seat) => seat.Toggle();

    public void UpdateSelection()
    {
        var selected = Rows.SelectMany(r => r.Seats).Where(s => s.IsSelected).ToList();
        SelectedCount = selected.Count;
        HasSelection  = selected.Count > 0;

        if (selected.Count == 0)
        {
            SelectionSummary = "Выберите места";
            TotalPrice       = 0;
            return;
        }

        var seats = string.Join(", ", selected.Select(s => $"{s.Row}{s.Number}"));
        TotalPrice       = selected.Sum(s => Session!.BasePrice * s.Seat.PriceModifier);
        SelectionSummary = $"Места: {seats} · {selected.Count} билет(а) · {TotalPrice:N0} ₽";
    }

    [RelayCommand]
    private void Purchase()
    {
        if (_main.AccountVm.CurrentUser == null)
        {
            _main.NavigateTo(AppPage.Account);
            return;
        }
        var selected = Rows.SelectMany(r => r.Seats).Where(s => s.IsSelected).ToList();
        if (!selected.Any()) return;

        using var db = new CinemaDbContext();
        foreach (var sv in selected)
        {
            db.Tickets.Add(new Ticket
            {
                UserId    = _main.AccountVm.CurrentUser.Id,
                SessionId = Session!.Id,
                SeatRow   = sv.Row,
                SeatNumber = sv.Number,
                SeatType  = sv.Type,
                Price     = Session.BasePrice * sv.Seat.PriceModifier,
                QrCode    = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                Status    = TicketStatus.Active,
                PurchasedAt = DateTime.UtcNow
            });
            var seat = db.Seats.Find(sv.Seat.Id);
            if (seat != null) seat.Status = SeatStatus.Occupied;
        }

        var user = db.Users.Find(_main.AccountVm.CurrentUser.Id);
        if (user != null) user.LoyaltyPoints += (int)(TotalPrice / 10);
        db.SaveChanges();

        System.Windows.MessageBox.Show(
            $"Бронирование успешно!\n\nКуплено билетов: {selected.Count}\nСумма: {TotalPrice:N0} ₽",
            "Оплата прошла",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        _main.NavigateTo(AppPage.MyTickets);
    }

    [RelayCommand]
    private void GoBack() => _main.NavigateTo(AppPage.Movies);
}
