using System.Globalization;

namespace agaverosActividades.Converters
{
    public class TabSelectedTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isSelected = value is bool b && b;
            return isSelected ? Colors.White : Color.FromArgb("#9E3700");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}