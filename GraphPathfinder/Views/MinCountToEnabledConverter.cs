using System.Globalization;
using System.Windows.Data;

namespace GraphPathfinder.Views
{
    public class MinCountToEnabledConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count && int.TryParse(parameter?.ToString(), out int min))
                return count >= min;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("One-way conversion from boolean to count is not supported.");
        }
    }
}
