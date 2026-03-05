using System.Globalization;
using System.Windows.Data;

namespace EndpointConsole.Wpf.Converters;

public sealed class ServiceStatusToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string statusText || string.IsNullOrWhiteSpace(statusText))
        {
            return "Unknown";
        }

        return statusText.Trim().ToLowerInvariant() switch
        {
            "running" => "Running",
            "stopped" => "Stopped",
            "paused" => "Paused",
            "startpending" => "Start Pending",
            "stoppending" => "Stop Pending",
            _ => statusText
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
