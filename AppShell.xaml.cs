using agaverosActividades.Views;

namespace agaverosActividades
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas implícitas: páginas a las que se navega por GoToAsync
            // pero que NO aparecen como ShellContent (tab/página fija) en el XAML.
            Routing.RegisterRoute(nameof(RegistroActividadesPage), typeof(RegistroActividadesPage));
            Routing.RegisterRoute(nameof(RegistroActividadFormPage), typeof(RegistroActividadFormPage));
            Routing.RegisterRoute(nameof(ExplosionRecetaPage), typeof(ExplosionRecetaPage));
            Routing.RegisterRoute(nameof(RegistroActividadPrepTerrenoFormPage), typeof(RegistroActividadPrepTerrenoFormPage));
            Routing.RegisterRoute(nameof(RegistroActividadesPrepTerrenoPage), typeof(RegistroActividadesPrepTerrenoPage));
            Routing.RegisterRoute(nameof(AutorizacionSuperiorPage), typeof(AutorizacionSuperiorPage));
            Routing.RegisterRoute(nameof(AutorizacionSuperiorDetallePage), typeof(AutorizacionSuperiorDetallePage));   // ← nueva
        }
    }
}