using agaverosActividades.Constants;
using agaverosActividades.Models;
using agaverosActividades.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;

namespace agaverosActividades.ViewModels
{
    public partial class BienvenidoViewModel : ObservableObject
    {
        private readonly LoginViewModel _loginViewModel;
        private readonly ILocalDataService _localDataService;

        [ObservableProperty]
        private string usuario = string.Empty;

        /// <summary>Cuántos registros están esperando sincronizarse. Se refresca en cada
        /// OnAppearing de Bienvenido y después de cada intento de sincronización.</summary>
        [ObservableProperty]
        private int totalPendientes;

        /// <summary>Texto del último resultado de sincronización, para mostrar en la pantalla
        /// (ej. "3 de 3 registros sincronizados" o "2 registros con error, revísalos").</summary>
        [ObservableProperty]
        private string? ultimoResultadoSincronizacion;

        [ObservableProperty]
        private string perfil = string.Empty;

        [ObservableProperty]
        private bool isTabOperacionSelected = true;

        [ObservableProperty]
        private bool isTabSincronizarSelected;

        [ObservableProperty]
        private bool isSubmenuRequisicionesVisible;

        [ObservableProperty]
        private bool estaSincronizando;

        [ObservableProperty]
        private string progresoTexto = string.Empty;

        // ── Eventos para comunicar al View (mismo patrón que LoginViewModel) ──
        public event Action? NavegandoALogin;
        public event Action? NavegandoAActividades;
        public event Action? NavegandoAAutorizacionSuperior;
        public event Action? NavegandoAActPrepTerreno;
        public event Action? NavegandoAAltaRequisicion;
        public event Func<string, string, Task>? MostrandoAlerta;
        private readonly IConnectivityMonitorService _connectivityMonitorService;

        [ObservableProperty]
        private int erroresPendientes;

        public BienvenidoViewModel(
            LoginViewModel loginViewModel,
            ILocalDataService localDataService,
            IConnectivityMonitorService connectivityMonitorService)
        {
            _loginViewModel = loginViewModel;
            _localDataService = localDataService;
            _connectivityMonitorService = connectivityMonitorService;

            _connectivityMonitorService.SincronizacionAutomaticaCompletada += OnSincronizacionAutomaticaAsync;

            CargarDatosUsuario();
        }

        // Reemplaza RefrescarPendientesAsync() por esta versión desglosada:
        public async Task RefrescarPendientesAsync()
        {
            var (pendientes, conError) = await _localDataService.ContarPorEstadoAsync();
            TotalPendientes = pendientes + conError;
            ErroresPendientes = conError;
        }

        // Handler silencioso: solo refresca el número, sin DisplayAlert (ver la razón
        // en el comentario de ConnectivityMonitorService).
        private async Task OnSincronizacionAutomaticaAsync(SincronizacionResultado resultado)
        {
            await RefrescarPendientesAsync();
        }

        public BienvenidoViewModel(LoginViewModel loginViewModel, ILocalDataService localDataService)
        {
            _loginViewModel = loginViewModel;
            _localDataService = localDataService;
            CargarDatosUsuario();
        }

        private void CargarDatosUsuario()
        {
            Usuario = Preferences.Get(PreferenceKeys.NOMBRE, string.Empty);
            Perfil = Preferences.Get(PreferenceKeys.PERFIL, string.Empty);
        }

        /// <summary>
        /// Se debe llamar cada vez que la página Bienvenido aparece
        /// (OnAppearing), para refrescar usuario/perfil por si cambiaron
        /// tras un nuevo login.
        /// </summary>
        public void RefrescarDatosUsuario()
        {
            CargarDatosUsuario();
        }

        // ── Tabs ─────────────────────────────────────────────────────
        [RelayCommand]
        private void SeleccionarOperacion()
        {
            IsTabOperacionSelected = true;
            IsTabSincronizarSelected = false;
        }

        [RelayCommand]
        private void SeleccionarSincronizar()
        {
            IsTabOperacionSelected = false;
            IsTabSincronizarSelected = true;
        }

        // ── Menú de opciones ─────────────────────────────────────────
        [RelayCommand]
        private void OpcionActividades()
        {
            NavegandoAActividades?.Invoke();
        }

        [RelayCommand]
        private void OpcionAutorizacionSuperior()
        {
            NavegandoAAutorizacionSuperior?.Invoke();
        }

        [RelayCommand]
        private void OpcionActPrepTerreno()
        {
            NavegandoAActPrepTerreno?.Invoke();
        }

        [RelayCommand]
        private void ToggleMenuRequisiciones()
        {
            IsSubmenuRequisicionesVisible = !IsSubmenuRequisicionesVisible;
        }

        [RelayCommand]
        private void OpcionAltaRequisicion()
        {
            NavegandoAAltaRequisicion?.Invoke();
        }

        // ── Sincronizar ──────────────────────────────────────────────
        // ── Sincronizar ──────────────────────────────────────────────
        [RelayCommand]
        private async Task Sincronizar()
        {
            // Se revisa conectividad ANTES de intentar, para no esperar timeouts
            // ni abrir conexiones HTTP que ya sabemos que van a fallar.
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                ProgresoTexto = string.Empty;
                if (MostrandoAlerta != null)
                    await MostrandoAlerta("Sin conexión", "No se detecta conexión a internet. Conéctate a una red con datos o WiFi e intenta de nuevo.");
                return;
            }

            if (TotalPendientes == 0)
            {
                if (MostrandoAlerta != null)
                    await MostrandoAlerta("Todo al día", "No hay registros pendientes de sincronizar.");
                return;
            }

            EstaSincronizando = true;
            ProgresoTexto = "Sincronizando...";

            try
            {
                var resultado = await _localDataService.SincronizarPendientesAsync();

                TotalPendientes = await _localDataService.ContarPendientesAsync();

                if (resultado.ConError > 0)
                {
                    UltimoResultadoSincronizacion =
                        $"{resultado.Exitosas} de {resultado.TotalIntentadas} registros sincronizados. " +
                        $"{resultado.ConError} con error — revísalos antes de reintentar.";
                }
                else if (resultado.Exitosas == resultado.TotalIntentadas && resultado.TotalIntentadas > 0)
                {
                    UltimoResultadoSincronizacion = $"{resultado.Exitosas} registro(s) sincronizado(s) correctamente.";
                }
                else
                {
                    // TotalIntentadas > Exitosas sin ConError => se perdió la conexión a medias.
                    UltimoResultadoSincronizacion = resultado.Exitosas > 0
                        ? $"Se sincronizaron {resultado.Exitosas} registro(s); se perdió la conexión con el resto. Vuelve a intentar."
                        : "No se pudo sincronizar: se perdió la conexión. Vuelve a intentar.";
                }

                ProgresoTexto = UltimoResultadoSincronizacion;

                if (MostrandoAlerta != null)
                    await MostrandoAlerta("Sincronización", UltimoResultadoSincronizacion);
            }
            catch (Exception)
            {
                ProgresoTexto = string.Empty;
                if (MostrandoAlerta != null)
                    await MostrandoAlerta("Error", "Ocurrió un problema al sincronizar. Intenta de nuevo.");
            }
            finally
            {
                EstaSincronizando = false;
            }
        }

        // ── Cerrar sesión ────────────────────────────────────────────
        [RelayCommand]
        private void CerrarSesion()
        {
            Preferences.Remove(PreferenceKeys.LOGIN);
            Preferences.Remove(PreferenceKeys.NOMBRE);
            Preferences.Remove(PreferenceKeys.PERFIL);
            Preferences.Remove(PreferenceKeys.MODO);

            _loginViewModel.LimpiarFormularioPublico();

            // Reset del estado propio de Bienvenido para la próxima sesión
            IsTabOperacionSelected = true;
            IsTabSincronizarSelected = false;
            IsSubmenuRequisicionesVisible = false;

            NavegandoALogin?.Invoke();
        }
    }
}