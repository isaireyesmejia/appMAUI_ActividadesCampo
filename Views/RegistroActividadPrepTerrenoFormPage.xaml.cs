using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class RegistroActividadPrepTerrenoFormPage : ContentPage
{
    private readonly RegistroActividadPrepTerrenoFormViewModel _viewModel;
    private bool _navegandoAImagen;

    public RegistroActividadPrepTerrenoFormPage(RegistroActividadPrepTerrenoFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        // Mismo patrón que RegistroActividadFormPage: el back físico de Android
        // reutiliza la confirmación y limpieza del botón "Cerrar".
        _viewModel.CerrarCommand.Execute(null);
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarAsync();

        // Se quita antes de volver a suscribir por la misma razón documentada en
        // RegistroActividadFormPage: OnDisappearing no siempre se dispara de forma
        // confiable al volver de un PushModalAsync, así que sin este patrón se
        // acumulan suscripciones duplicadas y se disparan varios PushModalAsync
        // simultáneos sobre la misma pila de navegación -> crash tras varios usos.
        _viewModel.SolicitarMostrarImagen -= OnSolicitarMostrarImagenAsync;
        _viewModel.SolicitarMostrarImagen += OnSolicitarMostrarImagenAsync;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.SolicitarMostrarImagen -= OnSolicitarMostrarImagenAsync;
    }

    /// <summary>
    /// Formatea "Horas prod." a H:MM solo cuando el campo pierde el foco (no en cada tecla).
    /// Evita reasignar Entry.Text durante la composición del teclado nativo, que es lo que
    /// dispara el bug conocido de androidx.emoji2 ("IllegalArgumentException: end should be
    /// < than charSequence length") al reformatear en vivo con TextChanged.
    /// </summary>
    private void EntryHorasProductivas_Unfocused(object sender, FocusEventArgs e)
    {
        var formateado = _viewModel.FormatearHoras(EntryHorasProductivas.Text);
        EntryHorasProductivas.Text = formateado;
        _viewModel.HorasProductivas = formateado;
    }
    private void EntryHorasMuertas_Unfocused(object sender, FocusEventArgs e)
    {
        var formateado = _viewModel.FormatearHoras(EntryHorasMuertas.Text);
        EntryHorasMuertas.Text = formateado;
        _viewModel.HorasMuertas = formateado;
    }
    private async Task OnSolicitarMostrarImagenAsync(string imagenPath)
    {
        if (string.IsNullOrEmpty(imagenPath))
            return;

        // Segunda red de seguridad: evita disparar dos PushModalAsync casi simultáneos
        // (doble-tap del usuario, o si el evento llegara a dispararse más de una vez).
        if (_navegandoAImagen) return;
        _navegandoAImagen = true;
        try
        {
            await Navigation.PushModalAsync(new NavigationPage(new VerImagenPage(imagenPath))
            {
                BarBackgroundColor = Colors.Black,
                BarTextColor = Colors.White
            });
        }
        finally
        {
            _navegandoAImagen = false;
        }
    }
}