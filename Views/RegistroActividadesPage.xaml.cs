using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class RegistroActividadesPage : ContentPage
{
    private readonly RegistroActividadesViewModel _vm;

    public RegistroActividadesPage(RegistroActividadesViewModel viewModel)
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