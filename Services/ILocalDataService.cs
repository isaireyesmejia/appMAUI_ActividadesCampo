// Services/ILocalDataService.cs
using agaverosActividades.Models;

namespace agaverosActividades.Services;

public interface ILocalDataService
{
    /// <summary>
    /// Intenta guardar contra el servidor. Si no hay conexión (HttpRequestException
    /// con StatusCode == null), encola el payload automáticamente y regresa
    /// Encolado = true, sin propagar la excepción. Si el servidor respondió con un
    /// error de negocio (StatusCode != null), NO encola: propaga la excepción para
    /// que el ViewModel la muestre tal como hace hoy en su catch (HttpRequestException ex).
    /// </summary>
    Task<GuardarResultado> GuardarAsync(GuardarRegistroActividadPayload payload);

    /// <summary>Cuántas operaciones están pendientes o con error (para el badge/contador en UI).</summary>
    Task<int> ContarPendientesAsync();
    /// <summary>
    /// Igual que ContarPendientesAsync pero desglosado: Pendiente es el estado normal
    /// (aún no ha habido oportunidad de subir), ConError requiere atención del usuario
    /// (el servidor rechazó el dato por algo que hay que corregir). La UI debe mostrarlos
    /// distinto — un badge que mezcla ambos no le dice al usuario si necesita actuar.
    /// </summary>
    Task<(int pendientes, int conError)> ContarPorEstadoAsync();

    Task<List<PendingOperation>> ObtenerPendientesAsync();

    /// <summary>
    /// Botón "Sincronizar ahora": recorre las pendientes en orden de creación y las
    /// reintenta una por una. Si una falla por falta de red, se detiene ahí. Si una
    /// falla por error de negocio, se marca Error con el mensaje y se sigue con la
    /// siguiente (no bloquea el resto de la cola).
    /// </summary>
    Task<SincronizacionResultado> SincronizarPendientesAsync();
}

/// <summary>Resultado de GuardarAsync: le dice al ViewModel si se guardó en línea o se encoló.</summary>
public class GuardarResultado
{
    public bool GuardadoEnLinea { get; set; }
    public bool Encolado { get; set; }
    public string? FolioServidor { get; set; } // null si se encoló
}

public class SincronizacionResultado
{
    public int TotalIntentadas { get; set; }
    public int Exitosas { get; set; }
    public int ConError { get; set; }
    public List<string> Errores { get; set; } = new();
}