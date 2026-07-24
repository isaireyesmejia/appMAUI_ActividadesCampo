// Services/LocalDataService.cs
using agaverosActividades.Models;
using System.Text.Json;

namespace agaverosActividades.Services;

public class LocalDataService : ILocalDataService
{
    private readonly IDatabaseService _databaseService;
    private readonly IActividadService _actividadService;

    public LocalDataService(IDatabaseService databaseService, IActividadService actividadService)
    {
        _databaseService = databaseService;
        _actividadService = actividadService;
    }

    public async Task<GuardarResultado> GuardarAsync(GuardarRegistroActividadPayload payload)
    {
        await _databaseService.ListoAsync;

        try
        {
            var folio = await EjecutarPayloadAsync(payload);
            return new GuardarResultado { GuardadoEnLinea = true, FolioServidor = folio };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            // Sin respuesta del servidor (sin red / timeout): se encola, no se propaga.
            await EncolarAsync(payload);
            return new GuardarResultado { Encolado = true };
        }
        // StatusCode != null (error de negocio): se propaga sin capturar; el ViewModel
        // lo muestra igual que hoy en su catch (HttpRequestException ex).
    }

    private async Task EncolarAsync(GuardarRegistroActividadPayload payload)
    {
        var registro = new PendingOperation
        {
            Id = Guid.NewGuid(),
            Estado = EstadoOperacion.Pendiente,
            TipoEntidad = TipoEntidadOperacion.RegistroActividad,
            PayloadJson = JsonSerializer.Serialize(payload),
            FechaCreacion = DateTime.Now,
            Intentos = 0
        };

        await _databaseService.Conexion.InsertAsync(registro);
    }

    public async Task<int> ContarPendientesAsync()
    {
        await _databaseService.ListoAsync;
        return await _databaseService.Conexion.Table<PendingOperation>()
            .Where(p => p.Estado == EstadoOperacion.Pendiente || p.Estado == EstadoOperacion.Error)
            .CountAsync();
    }
    public async Task<(int pendientes, int conError)> ContarPorEstadoAsync()
    {
        await _databaseService.ListoAsync;

        var pendientes = await _databaseService.Conexion.Table<PendingOperation>()
            .Where(p => p.Estado == EstadoOperacion.Pendiente)
            .CountAsync();

        var conError = await _databaseService.Conexion.Table<PendingOperation>()
            .Where(p => p.Estado == EstadoOperacion.Error)
            .CountAsync();

        return (pendientes, conError);
    }

    public async Task<List<PendingOperation>> ObtenerPendientesAsync()
    {
        await _databaseService.ListoAsync;
        return await _databaseService.Conexion.Table<PendingOperation>()
            .Where(p => p.Estado == EstadoOperacion.Pendiente || p.Estado == EstadoOperacion.Error)
            .OrderBy(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task<SincronizacionResultado> SincronizarPendientesAsync()
    {
        await _databaseService.ListoAsync;

        var resultado = new SincronizacionResultado();
        var pendientes = await ObtenerPendientesAsync();

        foreach (var operacion in pendientes)
        {
            resultado.TotalIntentadas++;

            GuardarRegistroActividadPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<GuardarRegistroActividadPayload>(operacion.PayloadJson);
            }
            catch (Exception)
            {
                operacion.Estado = EstadoOperacion.Error;
                operacion.MensajeError = "El registro guardado localmente está dañado y no se pudo leer.";
                operacion.Intentos++;
                await _databaseService.Conexion.UpdateAsync(operacion);
                resultado.ConError++;
                resultado.Errores.Add("Un registro pendiente está dañado.");
                continue;
            }

            if (payload is null)
            {
                resultado.ConError++;
                continue;
            }

            operacion.Estado = EstadoOperacion.Enviando;
            await _databaseService.Conexion.UpdateAsync(operacion);

            try
            {
                await EjecutarPayloadAsync(payload);

                // Éxito: se elimina de la cola.
                await _databaseService.Conexion.DeleteAsync(operacion);
                resultado.Exitosas++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                // Sin conexión otra vez: se regresa a Pendiente y se detiene el ciclo.
                operacion.Estado = EstadoOperacion.Pendiente;
                operacion.Intentos++;
                await _databaseService.Conexion.UpdateAsync(operacion);
                resultado.Errores.Add("Se perdió la conexión durante la sincronización.");
                break;
            }
            catch (HttpRequestException ex)
            {
                // Error de negocio real: se marca para revisión manual, se sigue con las demás.
                operacion.Estado = EstadoOperacion.Error;
                operacion.MensajeError = ex.Message;
                operacion.Intentos++;
                await _databaseService.Conexion.UpdateAsync(operacion);
                resultado.ConError++;
                resultado.Errores.Add($"{payload.Descripcion}: {ex.Message}");
            }
        }

        return resultado;
    }

    /// <summary>
    /// Reproduce la secuencia real de llamadas al API (equivalente a GuardarAltaAsync/
    /// GuardarEdicionAsync del ViewModel). Se usa tanto en el intento inicial (GuardarAsync)
    /// como en la sincronización posterior. Regresa el folio del servidor si fue un alta.
    /// </summary>
    private async Task<string?> EjecutarPayloadAsync(GuardarRegistroActividadPayload payload)
    {
        int registroActividadKey;
        string? folio = null;

        if (payload.Alta != null)
        {
            var altaResultado = await _actividadService.AltaRegistroActividadAsync(payload.Alta);
            registroActividadKey = altaResultado.IntAGRRegistroActividadOutKey;
            folio = altaResultado.VchIDOut;

            payload.ActividadRealizada.IntMovimiento = 1;
            payload.ActividadRealizada.IntAGRActividadRealizadaKey = 0;
        }
        else
        {
            registroActividadKey = payload.ActividadIdEdicion;
            await _actividadService.ActualizarRegistroActividadAsync(payload.Actualizacion!);

            payload.ActividadRealizada.IntMovimiento = 2;
            payload.ActividadRealizada.IntAGRActividadRealizadaKey = payload.ActividadRealizadaKeyOriginal;
        }

        string? rutaImagenFinal = payload.ImagenUrlRemotaSinCambios;
        string? nombreImagenFinal = payload.ImagenNombre;

        if (!string.IsNullOrEmpty(payload.ImagenPathLocal) && File.Exists(payload.ImagenPathLocal))
        {
            try
            {
                rutaImagenFinal = await _actividadService.SubirImagenAsync(
                    payload.ImagenPathLocal,
                    payload.ImagenNombre ?? Path.GetFileName(payload.ImagenPathLocal));
                nombreImagenFinal = payload.ImagenNombre;
            }
            catch (Exception)
            {
                rutaImagenFinal = string.Empty;
                nombreImagenFinal = string.Empty;
            }
        }

        payload.ActividadRealizada.IntAGRRegistroActividadLink = registroActividadKey;
        payload.ActividadRealizada.VchRutaArchivo = rutaImagenFinal ?? string.Empty;
        payload.ActividadRealizada.VchNombreImagen = nombreImagenFinal ?? string.Empty;

        await _actividadService.ActividadRealizadaAsync(payload.ActividadRealizada);

        foreach (var insumo in payload.Insumos)
        {
            insumo.IntAGRRegistroActividadLink = registroActividadKey;
            await _actividadService.InsumoUtilizadoAsync(insumo);
        }

        foreach (var implemento in payload.Implementos)
        {
            implemento.IntAGRRegistroActividadLink = registroActividadKey;
            await _actividadService.ImplementoUtilizadoAsync(implemento);
        }

        foreach (var mp in payload.MateriaPrima)
        {
            mp.IntAGRRegistroActividadLink = registroActividadKey;
            await _actividadService.MateriaPrimaUtilizadaAsync(mp);
        }

        return folio;
    }
}