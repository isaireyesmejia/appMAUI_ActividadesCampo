using Microsoft.Maui.Networking;
using agaverosActividades.Models;

namespace agaverosActividades.Services;

/// <summary>
/// Escucha Connectivity.ConnectivityChanged a nivel app (Singleton) y dispara
/// SincronizarPendientesAsync() automáticamente cuando el dispositivo RECUPERA
/// internet (transición de "sin internet" a "con internet" — no en cada evento
/// de conectividad, que puede dispararse varias veces sin que realmente cambie
/// el estado real de acceso a la red).
///
/// A propósito NO muestra ningún DisplayAlert: una sincronización automática
/// puede ocurrir mientras el usuario está llenando un formulario en otra
/// pantalla, e interrumpirlo con un diálogo en ese momento sería peor
/// experiencia que la sincronización manual (que sí avisa, porque el usuario
/// la pidió explícitamente). BienvenidoViewModel se suscribe al evento
/// SincronizacionAutomaticaCompletada solo para refrescar su contador en
/// silencio si el usuario está viendo esa pantalla en ese momento.
/// </summary>
public interface IConnectivityMonitorService
{
    /// <summary>Debe llamarse una sola vez al arrancar la app (App.xaml.cs).</summary>
    void Iniciar();

    event Func<SincronizacionResultado, Task>? SincronizacionAutomaticaCompletada;
}

public class ConnectivityMonitorService : IConnectivityMonitorService
{
    private readonly ILocalDataService _localDataService;
    private readonly IDatabaseService _databaseService;

    private bool _iniciado;
    private bool _sincronizando;
    private NetworkAccess _ultimoEstadoConocido = NetworkAccess.Unknown;

    public event Func<SincronizacionResultado, Task>? SincronizacionAutomaticaCompletada;

    public ConnectivityMonitorService(ILocalDataService localDataService, IDatabaseService databaseService)
    {
        _localDataService = localDataService;
        _databaseService = databaseService;
    }

    public void Iniciar()
    {
        if (_iniciado) return;
        _iniciado = true;

        _ultimoEstadoConocido = Connectivity.Current.NetworkAccess;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var estadoAnterior = _ultimoEstadoConocido;
        _ultimoEstadoConocido = e.NetworkAccess;

        bool recuperoConexion = estadoAnterior != NetworkAccess.Internet
            && e.NetworkAccess == NetworkAccess.Internet;

        // Guard _sincronizando: si el evento se dispara varias veces seguidas
        // (común en Android al cambiar de WiFi a datos), no se apilan sincronizaciones.
        if (!recuperoConexion || _sincronizando) return;

        _sincronizando = true;
        try
        {
            await _databaseService.ListoAsync;

            var pendientes = await _localDataService.ContarPendientesAsync();
            if (pendientes == 0) return; // nada que hacer, no molestamos con una sync vacía

            var resultado = await _localDataService.SincronizarPendientesAsync();

            if (SincronizacionAutomaticaCompletada != null)
                await SincronizacionAutomaticaCompletada(resultado);
        }
        catch (Exception ex)
        {
            // Falla silenciosa a propósito: si la sincronización automática no puede
            // completarse (p. ej. la "conexión recuperada" era solo momentánea), el
            // usuario siempre tiene el botón manual como respaldo. No lo interrumpimos
            // con un error de un proceso que él no inició explícitamente.
            System.Diagnostics.Debug.WriteLine($"ConnectivityMonitorService error: {ex}");
        }
        finally
        {
            _sincronizando = false;
        }
    }
}