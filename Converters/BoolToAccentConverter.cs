// Converters/BoolToAccentConverter.cs
using System.Globalization;

namespace agaverosActividades.Converters;

public class BoolToAccentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool esSeleccionado = value is bool b && b;
        return esSeleccionado ? Color.FromArgb("#9E3700") : Colors.LightGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}