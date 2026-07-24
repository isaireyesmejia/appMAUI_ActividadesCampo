using System.Globalization;

namespace agaverosActividades.Converters
{
    /// <summary>
    /// Igual que SelectedItemToBackgroundColorConverter, pero para el color de borde
    /// del Frame — usa el color primario de la app (#9E3700) cuando el item está seleccionado.
    /// </summary>
    public class SelectedItemToBorderColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is null || values[1] is null || !values[0].Equals(values[1]))
                return Colors.Transparent;

            return Color.FromArgb("#9E3700");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}