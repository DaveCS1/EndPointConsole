using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EndpointConsole.Wpf.Converters;

public sealed class SeverityToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var currentSeverity = ParseSeverity(value as string);
        var thresholdSeverity = ParseSeverity(parameter as string);

        return currentSeverity >= thresholdSeverity
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static int ParseSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return 0;
        }

        return severity.Trim().ToLowerInvariant() switch
        {
            "info" => 0,
            "warning" => 1,
            "error" => 2,
            "critical" => 3,
            _ => 0
        };
    }
}
