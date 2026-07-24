using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class AutorizacionSuperiorDetallePage : ContentPage
{
    private readonly AutorizacionSuperiorDetalleViewModel _viewModel;

    public AutorizacionSuperiorDetallePage(AutorizacionSuperiorDetalleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarAsync();
    }
}