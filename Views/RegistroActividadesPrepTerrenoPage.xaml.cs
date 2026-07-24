using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class RegistroActividadesPrepTerrenoPage : ContentPage
{
    private readonly RegistroActividadesPrepTerrenoViewModel _vm;

    public RegistroActividadesPrepTerrenoPage(RegistroActividadesPrepTerrenoViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}