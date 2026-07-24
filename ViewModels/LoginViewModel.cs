using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using agaverosActividades.Constants;
using agaverosActividades.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace agaverosActividades.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly ISesionApp _sesionApp;
        private readonly IUsuarioCacheService _usuarioCacheService;

        // ── Fields con [ObservableProperty] ─────────────────────────
        [ObservableProperty]
        private string usuario = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool esPassword = true;

        [ObservableProperty]
        private bool modoOnline;

        [ObservableProperty]
        private bool modoOffline;

        [ObservableProperty]
        private bool estaCargando;

        [ObservableProperty]
        private string versionApp = string.Empty;

        // 🔒 Propiedad computada para retornar el icono según el estado
        public string IconoPassword => EsPassword ? "ojo_abierto.png" : "ojo_cerrado.png";

        // ── Eventos para comunicar al View ───────────────────────────
        public event Action? NavegandoABienvenido;
        public event Func<string, string, Task>? MostrandoAlerta;

        public LoginViewModel(IAuthService authService, ISesionApp sesionApp, IUsuarioCacheService usuarioCacheService)
        {
            _authService = authService;
            _sesionApp = sesionApp;
            _usuarioCacheService = usuarioCacheService;
            VersionApp = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
        }

        // ── Se ejecutan automáticamente al cambiar la propiedad ──────
        partial void OnModoOnlineChanged(bool value)
        {
            if (value) Preferences.Set(PreferenceKeys.MODO, "Online");
        }

        partial void OnModoOfflineChanged(bool value)
        {
            if (value) Preferences.Set(PreferenceKeys.MODO, "Offline");
        }

        // ── Commands ─────────────────────────────────────────────────
        [RelayCommand]
        private void TogglePassword()
        {
            EsPassword = !EsPassword;
            OnPropertyChanged(nameof(IconoPassword));
        }

        [RelayCommand(CanExecute = nameof(PuedeIniciarSesion))]
        private async Task IniciarSesionAsync()
        {
            if (!Preferences.ContainsKey(PreferenceKeys.MODO))
            {
                await (MostrandoAlerta?.Invoke("Error", "Recuerda seleccionar el modo.") ?? Task.CompletedTask);
                return;
            }

            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
            {
                await (MostrandoAlerta?.Invoke("Error", "Ingrese usuario y contraseña.") ?? Task.CompletedTask);
                return;
            }

            string modo = Preferences.Get(PreferenceKeys.MODO, "");

            if (modo == "Online")
                await LoginOnlineAsync();
            else if (modo == "Offline")
                await LoginOfflineAsync();
        }

        private bool PuedeIniciarSesion() => !EstaCargando;

        private async Task LoginOnlineAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await (MostrandoAlerta?.Invoke("Error",
                    "Es necesario estar conectado a una red para continuar.") ?? Task.CompletedTask);
                return;
            }

            EstaCargando = true;
            IniciarSesionCommand.NotifyCanExecuteChanged();

            try
            {
                var resultado = await _authService.LoginAsync(Usuario, Password);

                if (resultado is null)
                {
                    await (MostrandoAlerta?.Invoke("Error", "Respuesta inválida del servidor.") ?? Task.CompletedTask);
                    return;
                }

                if (resultado.BitExiste)
                {
                    GuardarSesion(resultado.VchNombre, resultado.BitEsAdministrador);

                    // Cachea las credenciales localmente (hasheadas) para permitir
                    // login offline la próxima vez que no haya señal en campo.
                    await _usuarioCacheService.GuardarCredencialesAsync(
                        Usuario,
                        resultado.VchNombre,
                        resultado.BitEsAdministrador ? "Administrador" : "Supervisor",
                        Password);

                    LimpiarFormulario();
                    NavegandoABienvenido?.Invoke();
                }
                else
                {
                    await (MostrandoAlerta?.Invoke("Error", "El usuario o contraseña es incorrecta.") ?? Task.CompletedTask);
                }
            }
            catch (Exception ex)
            {
                await (MostrandoAlerta?.Invoke("Error", $"Error al conectar: {ex.Message}") ?? Task.CompletedTask);
            }
            finally
            {
                EstaCargando = false;
                IniciarSesionCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task LoginOfflineAsync()
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await (MostrandoAlerta?.Invoke("Advertencia",
                    "Solo se puede usar el modo offline sin conexión a Internet.") ?? Task.CompletedTask);
                return;
            }

            EstaCargando = true;
            IniciarSesionCommand.NotifyCanExecuteChanged();

            try
            {
                var usuarioCacheado = await _usuarioCacheService.ValidarCredencialesAsync(Usuario, Password);

                if (usuarioCacheado is null)
                {
                    await (MostrandoAlerta?.Invoke("Error",
                        "Usuario o contraseña incorrecta, o no hay datos guardados de una sesión previa en línea.") ?? Task.CompletedTask);
                    return;
                }

                GuardarSesion(usuarioCacheado.NombreUsuario, usuarioCacheado.Perfil == "Administrador");
                LimpiarFormulario();
                NavegandoABienvenido?.Invoke();
            }
            finally
            {
                EstaCargando = false;
                IniciarSesionCommand.NotifyCanExecuteChanged();
            }
        }

        private void GuardarSesion(string nombre, bool esAdministrador)
        {
            _sesionApp.GuardarSesion(Usuario, nombre, esAdministrador ? "Administrador" : "Supervisor");
        }

        // ── Limpieza del formulario ───────────────────────────────────
        private void LimpiarFormulario()
        {
            Usuario = string.Empty;
            Password = string.Empty;
            EsPassword = true;
            ModoOnline = false;
            ModoOffline = false;
        }

        /// <summary>
        /// Punto de entrada público para que otros ViewModels (ej. BienvenidoViewModel
        /// al cerrar sesión) puedan limpiar este formulario, ya que LoginViewModel
        /// está registrado como Singleton y conserva su estado entre sesiones.
        /// </summary>
        public void LimpiarFormularioPublico()
        {
            LimpiarFormulario();
        }
    }
}