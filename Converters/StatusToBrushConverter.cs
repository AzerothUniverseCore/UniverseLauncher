using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AzerothUniverseLauncher.Converters;

/// <summary>Convertit "online"/"offline" en couleur verte/rouge pour le point de statut.</summary>
public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Online = new(System.Windows.Media.Color.FromRgb(0x3E, 0xC9, 0x6B));
    private static readonly SolidColorBrush Offline = new(System.Windows.Media.Color.FromRgb(0xD9, 0x4A, 0x4A));

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value as string ?? "offline";
        return status.Equals("online", StringComparison.OrdinalIgnoreCase) ? Online : Offline;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
