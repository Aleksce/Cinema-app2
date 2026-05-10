using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace CinemaApp.ViewModels;

public class Promotion
{
    public string Title            { get; init; } = string.Empty;
    public string Subtitle         { get; init; } = string.Empty;
    public string Description      { get; init; } = string.Empty;
    public string HowToUse         { get; init; } = string.Empty;
    public string Badge            { get; init; } = string.Empty;
    public string PromoCode        { get; init; } = string.Empty;
    public string GradientFrom     { get; init; } = "#1C1C2E";
    public string GradientTo       { get; init; } = "#2C2C3E";
    public string Icon             { get; init; } = "🎬";
    public string ValidUntil       { get; init; } = string.Empty;
    public bool   HasPromoCode     => !string.IsNullOrEmpty(PromoCode);

    // Time-based validity
    public bool   IsCurrentlyValid { get; init; } = true;
    public string ValidityBadge    => IsCurrentlyValid ? "✓ Доступно сейчас" : "Сейчас недоступно";
}

public partial class PromotionsViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Promotion> _promotions = new();

    public PromotionsViewModel() => LoadPromotions();

    private void LoadPromotions()
    {
        var now  = DateTime.Now;
        var hour = now.Hour;
        var dow  = now.DayOfWeek;
        bool isWeekday  = dow is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        bool isMorning  = hour < 12;
        bool isNight    = hour >= 22;
        bool isTuesday  = dow == DayOfWeek.Tuesday;

        Promotions = new ObservableCollection<Promotion>
        {
            new()
            {
                Title            = "Happy Hours",
                Subtitle         = "−30% на утренние сеансы до 12:00",
                Description      = "На все сеансы, начинающиеся до 12:00 — скидка 30%. Идеально для тех, кто любит смотреть кино в тишине.",
                HowToUse         = "Введите промокод при покупке билета",
                Badge            = "−30%",
                PromoCode        = "HAPPY30",
                GradientFrom     = "#1A0A30",
                GradientTo       = "#2D1060",
                Icon             = "🌅",
                ValidUntil       = "Ежедневно до 12:00",
                IsCurrentlyValid = isMorning
            },
            new()
            {
                Title            = "Синема Клуб",
                Subtitle         = "1 балл = 10 ₽ → бесплатный билет",
                Description      = "Накапливайте баллы с каждой покупки. 1000 баллов = бесплатный билет в зал «Стандарт». Баллы не сгорают.",
                HowToUse         = "Авторизуйтесь в приложении — баллы начисляются автоматически",
                Badge            = "БАЛЛЫ",
                PromoCode        = "",
                GradientFrom     = "#1A0A0A",
                GradientTo       = "#3A0A14",
                Icon             = "⭐",
                ValidUntil       = "Постоянная программа",
                IsCurrentlyValid = true
            },
            new()
            {
                Title            = "День рождения",
                Subtitle         = "−50% в день рождения и ±1 день",
                Description      = "В день рождения, накануне и на следующий день — скидка 50% на любой билет любого формата.",
                HowToUse         = "Введите промокод, предъявите паспорт на кассе",
                Badge            = "−50%",
                PromoCode        = "BIRTHDAY50",
                GradientFrom     = "#0A1A0A",
                GradientTo       = "#0A3A1A",
                Icon             = "🎂",
                ValidUntil       = "В день рождения ±1 день",
                IsCurrentlyValid = true
            },
            new()
            {
                Title            = "Семейный поход",
                Subtitle         = "−20% при покупке от 4 билетов",
                Description      = "Купите 4 и более билетов на один сеанс — получите скидку 20% на всю корзину.",
                HowToUse         = "Введите промокод при выборе 4+ мест",
                Badge            = "4+ билета",
                PromoCode        = "FAMILY20",
                GradientFrom     = "#1A0A1A",
                GradientTo       = "#2A1040",
                Icon             = "👨‍👩‍👧‍👦",
                ValidUntil       = "Постоянная акция",
                IsCurrentlyValid = true
            },
            new()
            {
                Title            = "IMAX Вторник",
                Subtitle         = "IMAX по цене 2D каждый вторник",
                Description      = "Каждый вторник IMAX-зал доступен по цене стандартного 2D. Блокбастеры в максимальном качестве — по доступной цене.",
                HowToUse         = "Введите промокод при оформлении заказа во вторник",
                Badge            = "Каждый вторник",
                PromoCode        = "IMAX2D",
                GradientFrom     = "#0A1020",
                GradientTo       = "#0A2040",
                Icon             = "📺",
                ValidUntil       = "Каждый вторник",
                IsCurrentlyValid = isTuesday
            },
            new()
            {
                Title            = "Студентам",
                Subtitle         = "−25% по студенческому билету",
                Description      = "Студентам очной формы — скидка 25% в будние дни. Приходите учиться смотреть хорошее кино!",
                HowToUse         = "Введите промокод в будние дни",
                Badge            = "−25%",
                PromoCode        = "STUDENT25",
                GradientFrom     = "#1A1500",
                GradientTo       = "#302800",
                Icon             = "🎓",
                ValidUntil       = "Пн–Пт",
                IsCurrentlyValid = isWeekday
            },
            new()
            {
                Title            = "Ночной сеанс",
                Subtitle         = "−40% на сеансы после 22:00",
                Description      = "Поздние сеансы после 22:00 — за полцены. Атмосфера ночного кино: мало людей, максимум погружения.",
                HowToUse         = "Введите промокод при покупке билета на ночной сеанс",
                Badge            = "−40%",
                PromoCode        = "NIGHT40",
                GradientFrom     = "#0A0A1A",
                GradientTo       = "#0A0A30",
                Icon             = "🌙",
                ValidUntil       = "Ежедневно после 22:00",
                IsCurrentlyValid = isNight
            },
        };
    }

    [RelayCommand]
    private void CopyPromoCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        try
        {
            Clipboard.SetText(code);
            StatusMessage = $"Промокод «{code}» скопирован!";
            _ = Task.Delay(3000).ContinueWith(_ =>
                Application.Current.Dispatcher.Invoke(() => StatusMessage = string.Empty));
        }
        catch { }
    }
}
