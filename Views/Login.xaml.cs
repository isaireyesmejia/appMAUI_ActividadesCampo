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

    // ── Efecto visual de foco en los campos Usuario/Contraseña ──────
    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            border.Stroke = Color.FromArgb("#9E3700");
            border.StrokeThickness = 2;
        }
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            // El Entry de Contraseña está dentro de un Grid, no directo en el Border,
            // por eso se revisa también el Parent del Parent.
            var border = entry.Parent as Border ?? (entry.Parent?.Parent as Border);
            if (border != null)
            {
                border.Stroke = Application.Current!.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#2A2A2A")
                    : Color.FromArgb("#E0E0E0");
                border.StrokeThickness = 1;
            }
        }
    }
}