using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Poplar.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string input = value?.ToString() ?? "null";
        if (string.IsNullOrEmpty(value?.ToString())) input = "empty";

        if (parameter is string param)
        {
            bool invert = param.StartsWith('!');
            string target = invert ? param.Substring(1) : param;
            
            var allowedValues = target.Split('|');
            bool isMatch = false;

            foreach (var allowed in allowedValues)
            {
                if (allowed.Equals("null", StringComparison.OrdinalIgnoreCase) && value == null) isMatch = true;
                else if (allowed.Equals("empty", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(value?.ToString())) isMatch = true;
                else if (input.Equals(allowed, StringComparison.OrdinalIgnoreCase)) isMatch = true;
                
                if (isMatch) break;
            }

            return (isMatch ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
