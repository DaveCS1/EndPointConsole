using System.Globalization;
using System.Windows.Data;

namespace EndpointConsole.Wpf.Converters;

public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value is not null;

        if (parameter is string parameterText &&
            parameterText.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            result = !result;
        }

        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
