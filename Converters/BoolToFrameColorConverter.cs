// Converters/BoolToFrameColorConverter.cs
using System.Globalization;

namespace agaverosActividades.Converters;

public class BoolToFrameColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool habilitado = value is bool b && b;
        return habilitado ? Colors.White : Color.FromArgb("#F0F8FF"); // AliceBlue
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}