using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class Bienvenido : ContentPage
{
    private readonly BienvenidoViewModel _vm;

    public Bienvenido(BienvenidoViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _vm.RefrescarDatosUsuario();
        await _vm.RefrescarPendientesAsync();

        _vm.NavegandoALogin -= OnNavegandoALogin;
        _vm.NavegandoAActividades -= OnNavegandoAActividades;
        _vm.NavegandoAAutorizacionSuperior -= OnNavegandoAAutorizacionSuperior;
        _vm.NavegandoAActPrepTerreno -= OnNavegandoAActPrepTerreno;
        _vm.NavegandoAAltaRequisicion -= OnNavegandoAAltaRequisicion;
        _vm.MostrandoAlerta -= OnMostrandoAlerta;

        _vm.NavegandoALogin += OnNavegandoALogin;
        _vm.NavegandoAActividades += OnNavegandoAActividades;
        _vm.NavegandoAAutorizacionSuperior += OnNavegandoAAutorizacionSuperior;
        _vm.NavegandoAActPrepTerreno += OnNavegandoAActPrepTerreno;
        _vm.NavegandoAAltaRequisicion += OnNavegandoAAltaRequisicion;
        _vm.MostrandoAlerta += OnMostrandoAlerta;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.NavegandoALogin -= OnNavegandoALogin;
        _vm.NavegandoAActividades -= OnNavegandoAActividades;
        _vm.NavegandoAAutorizacionSuperior -= OnNavegandoAAutorizacionSuperior;
        _vm.NavegandoAActPrepTerreno -= OnNavegandoAActPrepTerreno;
        _vm.NavegandoAAltaRequisicion -= OnNavegandoAAltaRequisicion;
        _vm.MostrandoAlerta -= OnMostrandoAlerta;
    }

    private async void OnNavegandoALogin()
    {
        try
        {
            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }

    private async void OnNavegandoAActividades()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(RegistroActividadesPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }

    private async void OnNavegandoAAutorizacionSuperior()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(AutorizacionSuperiorPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }
    private async void OnNavegandoAActPrepTerreno()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(RegistroActividadesPrepTerrenoPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }
    private async void OnNavegandoAAltaRequisicion()
        => await DisplayAlert("Próximamente", "La pantalla de Alta de Requisición aún no está disponible.", "OK");

    private async Task OnMostrandoAlerta(string titulo, string mensaje)
    {
        await DisplayAlert(titulo, mensaje, "OK");
    }
}
