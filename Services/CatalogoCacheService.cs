using System.Text.Json;
using agaverosActividades.Models;

namespace agaverosActividades.Services;

public interface ICatalogoCacheService
{
    Task<List<T>> ObtenerAsync<T>(string nombreCatalogo, Func<CancellationToken, Task<List<T>>> obtenerEnLineaAsync, int timeoutMs = 5000);
}

public class CatalogoCacheService : ICatalogoCacheService
{
    private readonly IDatabaseService _databaseService;

    public CatalogoCacheService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<T>> ObtenerAsync<T>(string nombreCatalogo, Func<CancellationToken, Task<List<T>>> obtenerEnLineaAsync, int timeoutMs = 5000)
    {
        await _databaseService.ListoAsync;

        using var cts = new CancellationTokenSource();
        var tareaEnLinea = ObtenerEnLineaYGuardarAsync(nombreCatalogo, obtenerEnLineaAsync, cts.Token);
        var tareaTimeout = Task.Delay(timeoutMs, cts.Token);

        Task completadaPrimero;
        try
        {
            completadaPrimero = await Task.WhenAny(tareaEnLinea, tareaTimeout);
        }
        finally
        {
            // Evita que el Task.Delay siga vivo innecesariamente si tareaEnLinea ganó.
        }

        if (completadaPrimero == tareaEnLinea)
        {
            cts.Cancel(); // corta el Task.Delay pendiente, ya no se necesita
            try
            {
                return await tareaEnLinea;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                var listaCache = await LeerDeCacheAsync<T>(nombreCatalogo);
                if (listaCache != null)
                    return listaCache;

                throw;
            }
        }

        // Se cumplió el timeout: la API está tardando.
        var cacheInmediata = await LeerDeCacheAsync<T>(nombreCatalogo);

        if (cacheInmediata != null)
        {
            // Tenemos algo que mostrar ya: cancelamos la llamada real para no gastar recursos de más.
            cts.Cancel();
            _ = tareaEnLinea.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    System.Diagnostics.Debug.WriteLine($"Carga en segundo plano falló para {nombreCatalogo}: {t.Exception?.GetBaseException().Message}");
            }, TaskScheduler.Default);

            return cacheInmediata;
        }

        // No hay caché: dejamos que la llamada real siga viva, es la única fuente posible.
        try
        {
            return await tareaEnLinea;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw;
        }
    }

    private async Task<List<T>> ObtenerEnLineaYGuardarAsync<T>(
        string nombreCatalogo,
        Func<CancellationToken, Task<List<T>>> obtenerEnLineaAsync,
        CancellationToken token)
    {
        var lista = await obtenerEnLineaAsync(token);
        await GuardarEnCacheAsync(nombreCatalogo, lista);
        return lista;
    }

    private async Task GuardarEnCacheAsync<T>(string nombreCatalogo, List<T> lista)
    {
        var registro = new CatalogoCache
        {
            Nombre = nombreCatalogo,
            JsonData = JsonSerializer.Serialize(lista, JsonOptions.Default),
            FechaActualizacion = DateTime.Now
        };

        await _databaseService.Conexion.InsertOrReplaceAsync(registro);
    }

    private async Task<List<T>?> LeerDeCacheAsync<T>(string nombreCatalogo)
    {
        var registro = await _databaseService.Conexion.Table<CatalogoCache>()
            .Where(c => c.Nombre == nombreCatalogo)
            .FirstOrDefaultAsync();

        if (registro == null) return null;

        try
        {
            return JsonSerializer.Deserialize<List<T>>(registro.JsonData, JsonOptions.Default);
        }
        catch (Exception)
        {
            return null;
        }
    }
}