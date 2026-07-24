using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class Login : ContentPage
{
    private readonly LoginViewModel _vm;

    public Login(LoginViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Se protege contra doble-suscripción por si OnAppearing se llama
        // más de una vez sin un OnDisappearing intermedio.
        _vm.NavegandoABienvenido -= OnNavegandoABienvenido;
        _vm.MostrandoAlerta -= OnMostrandoAlerta;

        _vm.NavegandoABienvenido += OnNavegandoABienvenido;
        _vm.MostrandoAlerta += OnMostrandoAlerta;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.NavegandoABienvenido -= OnNavegandoABienvenido;
        _vm.MostrandoAlerta -= OnMostrandoAlerta;
    }

    private async void OnNavegandoABienvenido()
    {
        try
        {
            await Shell.Current.GoToAsync("//bienvenido");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
        }
    }

    private async Task OnMostrandoAlerta(string titulo, string mensaje)
    {
        await DisplayAlert(titulo, mensaje, "OK");
    }
}