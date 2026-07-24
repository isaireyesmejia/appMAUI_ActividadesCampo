namespace agaverosActividades.Views;

/// <summary>
/// Visor de imagen a pantalla completa (modal). Se abre desde "Ver" / tap en la miniatura
/// en RegistroActividadFormPage, vía el evento SolicitarMostrarImagen del ViewModel.
/// Soporta pinch-to-zoom, arrastre con el zoom activo, y doble tap para resetear.
/// </summary>
public partial class VerImagenPage : ContentPage
{
    // Estado del zoom/pan (mismo patrón recomendado por Microsoft Learn para PinchGestureRecognizer)
    private double _escalaActual = 1;
    private double _escalaInicio = 1;
    private double _xActual = 0;
    private double _yActual = 0;

    public VerImagenPage(string rutaImagen)
    {
        InitializeComponent();

        // ImageSource.FromFile funciona tanto para rutas locales (cache) como para paths absolutos.
        ImagenCompleta.Source = ImageSource.FromFile(rutaImagen);
        Cargando.IsVisible = false;
        Cargando.IsRunning = false;
    }

    private async void OnCerrarTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _escalaInicio = ContenedorZoom.Scale;
            ContenedorZoom.AnchorX = 0;
            ContenedorZoom.AnchorY = 0;
        }

        if (e.Status == GestureStatus.Running)
        {
            var escalaRenderizada = _escalaActual * e.Scale;
            escalaRenderizada = Math.Max(1, Math.Min(4, escalaRenderizada)); // límite 1x - 4x

            var pinchCentroX = e.ScaleOrigin.X * ContenedorZoom.Width;
            var pinchCentroY = e.ScaleOrigin.Y * ContenedorZoom.Height;

            var xAnclaAntes = ContenedorZoom.X + pinchCentroX * ContenedorZoom.Scale;
            var yAnclaAntes = ContenedorZoom.Y + pinchCentroY * ContenedorZoom.Scale;

            var xAnclaDespues = ContenedorZoom.X + pinchCentroX * escalaRenderizada;
            var yAnclaDespues = ContenedorZoom.Y + pinchCentroY * escalaRenderizada;

            _xActual += (xAnclaAntes - xAnclaDespues);
            _yActual += (yAnclaAntes - yAnclaDespues);

            ContenedorZoom.TranslationX = _xActual;
            ContenedorZoom.TranslationY = _yActual;
            ContenedorZoom.Scale = escalaRenderizada;
        }

        if (e.Status == GestureStatus.Completed)
        {
            _escalaActual = ContenedorZoom.Scale;
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        // Solo permitir arrastre si ya hay zoom aplicado; si no, se siente "pegado" sin motivo.
        if (ContenedorZoom.Scale <= 1) return;

        if (e.StatusType == GestureStatus.Running)
        {
            ContenedorZoom.TranslationX = _xActual + e.TotalX;
            ContenedorZoom.TranslationY = _yActual + e.TotalY;
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            _xActual = ContenedorZoom.TranslationX;
            _yActual = ContenedorZoom.TranslationY;
        }
    }

    private async void OnDobleTap(object sender, TappedEventArgs e)
    {
        // Doble tap resetea el zoom a su estado original.
        _escalaActual = 1;
        _xActual = 0;
        _yActual = 0;

        await ContenedorZoom.ScaleTo(1, 200, Easing.CubicOut);
        await Task.WhenAll(
            ContenedorZoom.TranslateTo(0, 0, 200, Easing.CubicOut)
        );
    }
}
