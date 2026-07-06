using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AzerothUniverseLauncher.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Passe "Invert" en ConverterParameter pour inverser la logique.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool bv && bv;
        if (parameter as string == "Invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
