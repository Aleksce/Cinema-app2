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

    // ── Promo code definitions ───────────────────────────────────────────────
    // Validate returns null if the promo is applicable, or an error message.
    private static readonly Dictionary<string, (int Percent, string Desc, Func<Session, int, string?> Validate)>
        PromoCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HAPPY30"]    = (30, "−30% на утренний сеанс",
            (s, _) => s.StartTime.Hour < 12
                ? null
                : "Действует только на сеансы, начинающиеся до 12:00"),

        ["BIRTHDAY50"] = (50, "−50% день рождения",
            (s, _) => null),   // self-certified — always accepted

        ["FAMILY20"]   = (20, "−20% семейный поход",
            (s, cnt) => cnt >= 4
                ? null
                : $"Нужно минимум 4 билета (выбрано {cnt})"),

        ["IMAX2D"]     = (33, "IMAX по цене 2D",
            (s, _) => s.Format == "IMAX"
                ? (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday
                    ? null
                    : "Действует только по вторникам")
                : "Действует только на IMAX-сеансы"),

        ["STUDENT25"]  = (25, "−25% студенческая скидка",
            (s, _) => DateTime.Now.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
                ? null
                : "Действует только в будние дни (Пн–Пт)"),

        ["NIGHT40"]    = (40, "−40% ночной сеанс",
            (s, _) => s.StartTime.Hour >= 22
                ? null
                : "Действует только на сеансы после 22:00"),
    };

    // ── State ────────────────────────────────────────────────────────────────
    [ObservableProperty] private Session? _session;
    [ObservableProperty] private ObservableCollection<SeatRowViewModel> _rows = new();
    [ObservableProperty] private string  _selectionSummary = "Выберите места";
    [ObservableProperty] private decimal _totalPrice;
    [ObservableProperty] private decimal _finalPrice;
    [ObservableProperty] private int     _selectedCount;
    [ObservableProperty] private bool    _hasSelection;
    [ObservableProperty] private bool    _allSeatsOccupied;

    // ── Promo code state ─────────────────────────────────────────────────────
    [ObservableProperty] private string _promoCodeInput  = string.Empty;
    [ObservableProperty] private string _promoStatus     = string.Empty;
    [ObservableProperty] private bool   _hasDiscount;
    [ObservableProperty] private int    _discountPercent;
    private string _appliedPromoCode = string.Empty;

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
        FinalPrice       = 0;
        SelectionSummary = "Выберите места";

        // Reset promo on new session
        PromoCodeInput   = string.Empty;
        PromoStatus      = string.Empty;
        HasDiscount      = false;
        DiscountPercent  = 0;
        _appliedPromoCode = string.Empty;

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
            FinalPrice       = 0;
            return;
        }

        var seatNames = string.Join(", ", selected.Select(s => $"{s.Row}{s.Number}"));
        TotalPrice    = selected.Sum(s => Session!.BasePrice * s.Seat.PriceModifier);

        if (HasDiscount && DiscountPercent > 0)
        {
            FinalPrice = Math.Floor(TotalPrice * (100 - DiscountPercent) / 100m);
            SelectionSummary = $"Места: {seatNames} · {selected.Count} билет(а)";
        }
        else
        {
            FinalPrice = TotalPrice;
            SelectionSummary = $"Места: {seatNames} · {selected.Count} билет(а)";
        }

        // Re-validate FAMILY20 when seat count changes
        if (HasDiscount && !string.IsNullOrEmpty(_appliedPromoCode)
            && PromoCodes.TryGetValue(_appliedPromoCode, out var promo))
        {
            var recheck = promo.Validate(Session!, SelectedCount);
            if (recheck != null)
            {
                HasDiscount     = false;
                DiscountPercent = 0;
                FinalPrice      = TotalPrice;
                PromoStatus     = $"❌ {recheck}";
            }
        }
    }

    [RelayCommand]
    private void ApplyPromoCode()
    {
        var code = PromoCodeInput?.Trim().ToUpper() ?? "";

        if (string.IsNullOrEmpty(code))
        {
            PromoStatus = "Введите промокод";
            return;
        }

        if (!PromoCodes.TryGetValue(code, out var promo))
        {
            PromoStatus     = "❌ Промокод не найден";
            HasDiscount     = false;
            DiscountPercent = 0;
            FinalPrice      = TotalPrice;
            return;
        }

        if (Session == null)
        {
            PromoStatus = "❌ Сеанс не выбран";
            return;
        }

        var error = promo.Validate(Session, SelectedCount);
        if (error != null)
        {
            PromoStatus     = $"❌ {error}";
            HasDiscount     = false;
            DiscountPercent = 0;
            FinalPrice      = TotalPrice;
            return;
        }

        DiscountPercent   = promo.Percent;
        HasDiscount       = true;
        _appliedPromoCode = code;
        PromoStatus       = $"✓ {promo.Desc} применён";
        UpdateSelection();
    }

    [RelayCommand]
    private void Purchase()
    {
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

        if (!selected.Any())
        {
            System.Windows.MessageBox.Show(
                "Выберите хотя бы одно место.",
                "Нет выбранных мест",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (Session != null && Session.StartTime < DateTime.Now)
        {
            System.Windows.MessageBox.Show(
                "Нельзя купить билет на прошедший или уже начавшийся сеанс.",
                "Сеанс недоступен",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var payAmount   = FinalPrice;
        var discountLine = HasDiscount
            ? $"\nСкидка {DiscountPercent}%: −{TotalPrice - FinalPrice:N0} ₽"
            : string.Empty;

        var confirm = System.Windows.MessageBox.Show(
            $"Подтвердить покупку?\n\nМеста: {string.Join(", ", selected.Select(s => $"{s.Row}{s.Number}"))}" +
            $"\nБез скидки: {TotalPrice:N0} ₽{discountLine}\n\nИтого к оплате: {payAmount:N0} ₽",
            "Подтверждение оплаты",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

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

        var purchasedAt  = DateTime.UtcNow;
        // Price per ticket with discount applied proportionally
        var priceMultiplier = HasDiscount ? (100 - DiscountPercent) / 100m : 1m;

        foreach (var sv in selected)
        {
            db.Tickets.Add(new Ticket
            {
                UserId      = _main.AccountVm.CurrentUser.Id,
                SessionId   = Session!.Id,
                SeatRow     = sv.Row,
                SeatNumber  = sv.Number,
                SeatType    = sv.Type,
                Price       = Math.Floor(Session.BasePrice * sv.Seat.PriceModifier * priceMultiplier),
                QrCode      = $"CINEMA-{Session.Id}-{sv.Row}{sv.Number}-{Guid.NewGuid():N}"[..28].ToUpper(),
                Status      = TicketStatus.Active,
                PurchasedAt = purchasedAt
            });

            var seat = db.Seats.Find(sv.Seat.Id);
            if (seat != null) seat.Status = SeatStatus.Occupied;
        }

        // Award loyalty points based on amount paid
        var user = db.Users.Find(_main.AccountVm.CurrentUser.Id);
        if (user != null)
        {
            user.LoyaltyPoints += (int)(payAmount / 10);
            _main.AccountVm.CurrentUser.LoyaltyPoints = user.LoyaltyPoints;
        }
        db.SaveChanges();

        System.Windows.MessageBox.Show(
            $"✅ Бронирование успешно!\n\nКуплено билетов: {selected.Count}\nСумма: {payAmount:N0} ₽\n\n" +
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
