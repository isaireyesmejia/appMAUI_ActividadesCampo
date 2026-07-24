// Converters/IntComparacionConverters.cs
using System.Globalization;

namespace agaverosActividades.Converters;

/// <summary>True si el entero es mayor que cero (para mostrar badges/avisos de "hay pendientes").</summary>
public class IntMayorQueCeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>True si el entero es cero (para mostrar el mensaje de "todo sincronizado").</summary>
public class IntEsCeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}