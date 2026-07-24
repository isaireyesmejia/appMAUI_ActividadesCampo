using agaverosActividades.ViewModels;

namespace agaverosActividades.Views;

public partial class ExplosionRecetaPage : ContentPage
{
    public ExplosionRecetaPage(ExplosionRecetaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}