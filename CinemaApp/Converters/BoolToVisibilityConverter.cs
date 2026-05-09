using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CinemaApp.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString() == "invert";
        bool boolVal = value switch
        {
            bool b   => b,
            string s => !string.IsNullOrEmpty(s),
            _        => value != null
        };
        if (invert) boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}
