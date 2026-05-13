using System.Windows.Controls;

namespace Poplar.Helpers;

internal sealed class StringToValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ComboBoxItem item)
        {
            return item.Tag;
        }

        return value;
    }
}
