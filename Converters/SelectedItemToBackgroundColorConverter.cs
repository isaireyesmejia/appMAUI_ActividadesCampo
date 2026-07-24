using System.Globalization;

namespace agaverosActividades.Converters
{
    /// <summary>
    /// Compara el item actual del ItemTemplate contra el item seleccionado del ViewModel
    /// (pasados vía MultiBinding) y regresa el color de fondo correspondiente,
    /// respetando tema claro/oscuro.
    /// values[0] = item actual (BindingContext del template)
    /// values[1] = item seleccionado (ActividadSeleccionada del ViewModel)
    /// </summary>
    public class SelectedItemToBackgroundColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool esOscuro = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (values.Length < 2 || values[0] is null || values[1] is null || !values[0].Equals(values[1]))
                return esOscuro ? Color.FromArgb("#2A2A2A") : Colors.AliceBlue;

            return esOscuro ? Color.FromArgb("#4A2E22") : Color.FromArgb("#F5DDCF");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}