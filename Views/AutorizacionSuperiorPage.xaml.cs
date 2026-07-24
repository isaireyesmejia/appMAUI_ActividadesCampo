using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class AutorizacionSuperiorPage : ContentPage
{
    private readonly AutorizacionSuperiorViewModel _vm;

    public AutorizacionSuperiorPage(AutorizacionSuperiorViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override bool OnBackButtonPressed()
    {
        _vm.CerrarCommand.Execute(null);
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}