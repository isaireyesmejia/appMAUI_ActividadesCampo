using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class RegistroActividadFormPage : ContentPage
{
    private readonly RegistroActividadFormViewModel _vm;
    private bool _navegandoAImagen;

    public RegistroActividadFormPage(RegistroActividadFormViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }
    protected override bool OnBackButtonPressed()
    {
        // Reutiliza exactamente la misma lógica de confirmación y limpieza que el botón "Cerrar"
        // de la UI, para que el back físico de Android tenga el mismo comportamiento.
        _vm.CerrarCommand.Execute(null);
        return true; // true = nosotros manejamos la navegación, evita que Android navegue por su cuenta
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();

        // Se quita antes de volver a suscribir: OnDisappearing no siempre se dispara de forma
        // confiable cuando se regresa de un PushModalAsync (comportamiento conocido de MAUI/Shell),
        // así que sin esto se iban acumulando suscripciones duplicadas cada vez que se abría la
        // imagen, y con varias suscripciones activas se disparaban PushModalAsync simultáneos
        // sobre la misma pila de navegación -> crash tras varios usos.
        _vm.SolicitarMostrarImagen -= OnSolicitarMostrarImagen;
        _vm.SolicitarMostrarImagen += OnSolicitarMostrarImagen;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.SolicitarMostrarImagen -= OnSolicitarMostrarImagen;
    }

    private async Task OnSolicitarMostrarImagen(string path)
    {
        // Segunda red de seguridad: evita disparar dos PushModalAsync casi simultáneos
        // (doble-tap del usuario, o si el evento llegara a dispararse más de una vez).
        if (_navegandoAImagen) return;
        _navegandoAImagen = true;
        try
        {
            await Navigation.PushModalAsync(new NavigationPage(new VerImagenPage(path))
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
