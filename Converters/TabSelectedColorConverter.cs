using System.Globalization;

namespace agaverosActividades.Converters
{
    public class TabSelectedColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isSelected = value is bool b && b;
            return isSelected ? Color.FromArgb("#9E3700") : Colors.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}