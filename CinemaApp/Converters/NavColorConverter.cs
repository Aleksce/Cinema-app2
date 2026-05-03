using CinemaApp.ViewModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CinemaApp.Converters;

public class NavColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppPage page && parameter is string pageName)
        {
            if (Enum.TryParse<AppPage>(pageName, out var targetPage) && page == targetPage)
                return new SolidColorBrush(Color.FromRgb(0xE5, 0x09, 0x14));
        }
        return new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0xA0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
