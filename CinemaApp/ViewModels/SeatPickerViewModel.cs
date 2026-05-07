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

    [ObservableProperty] private bool _isSelected;

    public string     Row    => Seat.Row;
    public int        Number => Seat.Number;
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
    public string                              RowLabel { get; }
    public ObservableCollection<SeatViewModel> Seats    { get; }

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
    [ObservableProperty] private bool    _allSeatsOccupied;

    public SeatPickerViewModel(MainViewModel main) => _main = main;

    public void LoadSession(Session session) => _ = LoadSessionAsync(session);

    public async Task LoadSessionAsync(Session session)
    {
        Session          = session;
        IsBusy           = true;
        AllSeatsOccupied = false;
        Rows             = new();
        HasSelection     = false;
        TotalPrice       = 0;
        SelectionSummary = "Выберите места";

        try
        {
            var seats = await Task.Run(() =>
            {
                using var db = new CinemaDbContext();

                var fullSession = db.Sessions
                    .Include(s => s.Hall)
                    .Include(s => s.Movie)
                    .FirstOrDefault(s => s.Id == session.Id);

                if (fullSession == null) return new List<Seat>();

                MovieSyncService.EnsureSeatsExist(db, fullSession);

                return db.Seats
                    .Where(s => s.SessionId == session.Id)
                    .OrderBy(s => s.Row)
                    .ThenBy(s => s.Number)
                    .ToList();
            });

            // Edge case: no seats generated (hall misconfigured)
            if (!seats.Any())
            {
                AllSeatsOccupied = true;
                SelectionSummary = "Места недоступны для этого сеанса";
                return;
            }

            var vms     = seats.Select(s => new SeatViewModel(s, this)).ToList();
            var grouped = vms.GroupBy(s => s.Row)
                             .Select(g => new SeatRowViewModel(g.Key, g))
                             .ToList();
            Rows = new ObservableCollection<SeatRowViewModel>(grouped);
            UpdateSelection();

            // Edge case: every seat is taken
            AllSeatsOccupied = seats.All(s => s.Status == SeatStatus.Occupied);
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

        var seatNames    = string.Join(", ", selected.Select(s => $"{s.Row}{s.Number}"));
        TotalPrice       = selected.Sum(s => Session!.BasePrice * s.Seat.PriceModifier);
        SelectionSummary = $"Места: {seatNames} · {selected.Count} билет(а) · {TotalPrice:N0} ₽";
    }

    [RelayCommand]
    private void Purchase()
    {
        // Guard: not logged in
        if (_main.AccountVm.CurrentUser == null)
        {
            System.Windows.MessageBox.Show(
                "Войдите в аккаунт, чтобы купить билеты.",
                "Требуется авторизация",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            _main.NavigateTo(AppPage.Account);
            return;
        }

        var selected = Rows.SelectMany(r => r.Seats).Where(s => s.IsSelected).ToList();

        // Guard: nothing selected
        if (!selected.Any())
        {
            System.Windows.MessageBox.Show(
                "Выберите хотя бы одно место.",
                "Нет выбранных мест",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Guard: session already started
        if (Session != null && Session.StartTime < DateTime.Now)
        {
            System.Windows.MessageBox.Show(
                "Нельзя купить билет на прошедший или уже начавшийся сеанс.",
                "Сеанс недоступен",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Confirm
        var confirm = System.Windows.MessageBox.Show(
            $"Подтвердить покупку?\n\n{SelectionSummary}",
            "Подтверждение оплаты",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        // Re-check in DB for race conditions
        var seatIds = selected.Select(s => s.Seat.Id).ToList();
        using var db = new CinemaDbContext();

        var recheck      = db.Seats.Where(s => seatIds.Contains(s.Id)).ToList();
        var alreadyTaken = recheck.Where(s => s.Status == SeatStatus.Occupied).ToList();
        if (alreadyTaken.Any())
        {
            var names = string.Join(", ", alreadyTaken.Select(s => $"{s.Row}{s.Number}"));
            System.Windows.MessageBox.Show(
                $"Места {names} только что заняли другие покупатели. Пожалуйста, выберите другие места.",
                "Места недоступны",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            LoadSession(Session!);
            return;
        }

        var purchasedAt = DateTime.UtcNow;

        foreach (var sv in selected)
        {
            db.Tickets.Add(new Ticket
            {
                UserId      = _main.AccountVm.CurrentUser.Id,
                SessionId   = Session!.Id,
                SeatRow     = sv.Row,
                SeatNumber  = sv.Number,
                SeatType    = sv.Type,
                Price       = Session.BasePrice * sv.Seat.PriceModifier,
                QrCode      = $"CINEMA-{Session.Id}-{sv.Row}{sv.Number}-{Guid.NewGuid():N}"[..28].ToUpper(),
                Status      = TicketStatus.Active,
                PurchasedAt = purchasedAt
            });

            var seat = db.Seats.Find(sv.Seat.Id);
            if (seat != null) seat.Status = SeatStatus.Occupied;
        }

        // Award loyalty points
        var user = db.Users.Find(_main.AccountVm.CurrentUser.Id);
        if (user != null)
        {
            user.LoyaltyPoints += (int)(TotalPrice / 10);
            _main.AccountVm.CurrentUser.LoyaltyPoints = user.LoyaltyPoints;
        }
        db.SaveChanges();

        var count    = selected.Count;
        var totalStr = TotalPrice.ToString("N0");

        System.Windows.MessageBox.Show(
            $"✅ Бронирование успешно!\n\nКуплено билетов: {count}\nСумма: {totalStr} ₽\n\n" +
            "Билеты доступны в разделе «Мои билеты».\n" +
            "Там вы можете скачать PDF-чек с QR-кодом.",
            "Оплата прошла",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        _main.NavigateTo(AppPage.MyTickets);
    }

    [RelayCommand]
    private void GoBack() => _main.NavigateTo(AppPage.Movies);
}
