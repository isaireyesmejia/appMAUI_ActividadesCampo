using Microsoft.Extensions.Logging;
using agaverosActividades.Services;
using agaverosActividades.ViewModels;
using agaverosActividades.Views;
using agaverosActividades.Constants;

namespace agaverosActividades
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrar servicios
            builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.BaseAddress = new Uri(ApiEndpoints.BASE_API);
            });

            builder.Services.AddHttpClient<IActividadService, ActividadService>(client =>
            {
                client.BaseAddress = new Uri(ApiEndpoints.BASE_API);
            });

            builder.Services.AddSingleton<ISesionApp, SesionApp>();
            builder.Services.AddTransient<IMediaService, MediaService>();

            // Registrar ViewModels
            builder.Services.AddSingleton<LoginViewModel>();
            builder.Services.AddSingleton<BienvenidoViewModel>();

            // Registrar Páginas
            builder.Services.AddSingleton<Login>();
            builder.Services.AddSingleton<Bienvenido>();

            builder.Services.AddTransient<RegistroActividadesViewModel>();
            builder.Services.AddTransient<RegistroActividadesPage>();

            builder.Services.AddTransient<RegistroActividadPrepTerrenoFormPage>();
            builder.Services.AddTransient<RegistroActividadPrepTerrenoFormViewModel>();

            builder.Services.AddTransient<RegistroActividadFormViewModel>();
            builder.Services.AddTransient<RegistroActividadFormPage>();
            builder.Services.AddTransient<ExplosionRecetaViewModel>();
            builder.Services.AddTransient<ExplosionRecetaPage>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<ISessionService, SessionService>();
            builder.Services.AddSingleton<IUsuarioCacheService, UsuarioCacheService>();
            builder.Services.AddSingleton<ILocalDataService, LocalDataService>();
            builder.Services.AddSingleton<IConnectivityMonitorService, ConnectivityMonitorService>();
            builder.Services.AddSingleton<ICatalogoCacheService, CatalogoCacheService>();

            builder.Services.AddTransient<RegistroActividadesPrepTerrenoPage>();
            builder.Services.AddTransient<RegistroActividadesPrepTerrenoViewModel>();

            builder.Services.AddTransient<AutorizacionSuperiorPage>();
            builder.Services.AddTransient<AutorizacionSuperiorViewModel>();
            builder.Services.AddTransient<AutorizacionSuperiorDetallePage>();
            builder.Services.AddTransient<AutorizacionSuperiorDetalleViewModel>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}