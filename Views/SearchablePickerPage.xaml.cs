using System.Collections.ObjectModel;
using agaverosActividades.Models;

namespace agaverosActividades.Views;

/// <summary>
/// Selector genérico y reusable con buscador, para reemplazar Pickers nativos
/// que tienen listas largas (ej. Preparaciones, Predios, Vehículos, Actividades...).
///
/// Uso:
///   var seleccion = await SearchablePickerPage.MostrarAsync(
///       titulo: "Preparación",
///       items: ViewModel.Preparaciones,
///       textoMostrar: p => p.VchNombreComun);
///
///   if (seleccion != null)
///       ViewModel.PreparacionSeleccionada = seleccion;
/// </summary>
public partial class SearchablePickerPage : ContentPage
{
    public string Titulo { get; set; } = string.Empty;
    public ObservableCollection<SearchablePickerItem> OpcionesFiltradas { get; } = new();
    public Command ComandoCancelar { get; }

    private List<SearchablePickerItem> _todasLasOpciones = new();
    private readonly TaskCompletionSource<object?> _tcs = new();

    public SearchablePickerPage()
    {
        InitializeComponent();
        ComandoCancelar = new Command(async () => await CerrarAsync(null));
        BindingContext = this;
    }

    /// <summary>
    /// Abre el selector modal y espera a que el usuario elija un elemento o cancele.
    /// Devuelve el elemento seleccionado, o default(T) si canceló.
    /// </summary>
    public static async Task<T?> MostrarAsync<T>(string titulo, IEnumerable<T> items, Func<T, string> textoMostrar)
    {
        var pagina = new SearchablePickerPage { Titulo = titulo };

        pagina._todasLasOpciones = items
            .Where(i => i != null)
            .Select(i => new SearchablePickerItem { Valor = i!, Texto = textoMostrar(i) })
            .ToList();

        pagina.MostrarOpciones(pagina._todasLasOpciones);

        var navegacion = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (navegacion == null)
            return default;

        await navegacion.PushModalAsync(pagina, false);

        var resultado = await pagina._tcs.Task;
        return resultado is T tipado ? tipado : default;
    }

    private void MostrarOpciones(IEnumerable<SearchablePickerItem> opciones)
    {
        OpcionesFiltradas.Clear();
        foreach (var opcion in opciones)
            OpcionesFiltradas.Add(opcion);
    }

    private void EntryBusqueda_TextChanged(object sender, TextChangedEventArgs e)
    {
        var texto = (e.NewTextValue ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(texto))
        {
            MostrarOpciones(_todasLasOpciones);
            return;
        }

        var filtradas = _todasLasOpciones
            .Where(o => o.Texto?.Contains(texto, StringComparison.OrdinalIgnoreCase) == true);

        MostrarOpciones(filtradas);
    }

    private async void Opcion_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is SearchablePickerItem item)
            await CerrarAsync(item.Valor);
    }

    private async Task CerrarAsync(object? valor)
    {
        await Navigation.PopModalAsync(false);
        _tcs.TrySetResult(valor);
    }
}
