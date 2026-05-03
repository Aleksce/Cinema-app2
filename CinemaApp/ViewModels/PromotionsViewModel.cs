using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CinemaApp.ViewModels;

public class Promotion
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Badge { get; init; } = string.Empty;
    public string GradientFrom { get; init; } = "#1C1C2E";
    public string GradientTo { get; init; } = "#2C2C3E";
    public string Icon { get; init; } = "🎬";
    public string ValidUntil { get; init; } = string.Empty;
}

public partial class PromotionsViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Promotion> _promotions = new();

    public PromotionsViewModel()
    {
        LoadPromotions();
    }

    private void LoadPromotions()
    {
        Promotions = new ObservableCollection<Promotion>
        {
            new()
            {
                Title = "Happy Hours",
                Subtitle = "Скидка 30% на утренние сеансы",
                Description = "На все сеансы до 12:00 — скидка 30%. Идеально для ранних пташек! Акция действует ежедневно на любой зал и любой формат.",
                Badge = "−30%",
                GradientFrom = "#1A0A30",
                GradientTo = "#2D1060",
                Icon = "🌅",
                ValidUntil = "Постоянная акция"
            },
            new()
            {
                Title = "Синема Клуб",
                Subtitle = "Программа лояльности",
                Description = "Копите баллы с каждой покупки: 1 балл за каждые 10 ₽. При накоплении 1000 баллов получайте бесплатный билет на любой сеанс в зале «Стандарт».",
                Badge = "1000 баллов = билет",
                GradientFrom = "#1A0A0A",
                GradientTo = "#3A0A14",
                Icon = "⭐",
                ValidUntil = "Постоянная акция"
            },
            new()
            {
                Title = "День рождения",
                Subtitle = "Скидка 50% в день рождения",
                Description = "В свой день рождения (и за день до, и после) получи скидку 50% на любой билет. Покажи документ на кассе или предъяви карту лояльности.",
                Badge = "−50%",
                GradientFrom = "#0A1A0A",
                GradientTo = "#0A3A1A",
                Icon = "🎂",
                ValidUntil = "В день рождения"
            },
            new()
            {
                Title = "Семейный поход",
                Subtitle = "Семье из 4 человек — скидка 20%",
                Description = "При покупке от 4 билетов на один сеанс автоматически применяется скидка 20%. Приходите с семьёй или друзьями!",
                Badge = "4+ билета −20%",
                GradientFrom = "#1A0A1A",
                GradientTo = "#2A1040",
                Icon = "👨‍👩‍👧‍👦",
                ValidUntil = "Постоянная акция"
            },
            new()
            {
                Title = "IMAX Вторник",
                Subtitle = "IMAX по цене 2D по вторникам",
                Description = "Каждый вторник — особые цены на зал IMAX. Смотри блокбастеры в лучшем качестве по стандартной цене. Количество мест ограничено!",
                Badge = "Каждый вторник",
                GradientFrom = "#0A1020",
                GradientTo = "#0A2040",
                Icon = "📺",
                ValidUntil = "Каждый вторник"
            },
            new()
            {
                Title = "Студентам",
                Subtitle = "Скидка 25% по студенческому",
                Description = "Студентам очной формы обучения — скидка 25% на любой сеанс при предъявлении студенческого билета. Действует в будние дни.",
                Badge = "−25% студентам",
                GradientFrom = "#1A1500",
                GradientTo = "#302800",
                Icon = "🎓",
                ValidUntil = "Пн–Пт"
            },
        };
    }
}
