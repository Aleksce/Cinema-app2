using CinemaApp.Models;
using CinemaApp.ViewModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CinemaApp.Converters;

public class SeatColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SeatViewModel sv)
        {
            if (sv.Status == SeatStatus.Occupied)
                return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x4A));
            if (sv.IsSelected)
                return new SolidColorBrush(Color.FromRgb(0xE5, 0x09, 0x14));
            if (sv.Type == SeatType.VIP)
                return new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x10));
            if (sv.Type == SeatType.Sofa)
                return new SolidColorBrush(Color.FromRgb(0x1A, 0x32, 0x2A));
            return new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5C));
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
