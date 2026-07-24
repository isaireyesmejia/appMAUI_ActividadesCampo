namespace agaverosActividades.Models.Actividades;

/// <summary>
/// Item de UI para RegistroActividadesPage. Combina dos fuentes: registros reales
/// del servidor (RegistroOriginal != null) y altas capturadas offline que aún viven
/// solo como PendingOperation en el outbox (EsPendienteSincronizar = true,
/// RegistroOriginal = null, sin IntAGRRegistroActividadKey todavía).
/// </summary>
public class ActividadListItem
{
    public int? IntAGRRegistroActividadKey { get; set; }

    public string VchID { get; set; } = string.Empty;

    public DateTime DtmFecha { get; set; }

    public string VchEstatus { get; set; } = string.Empty;

    public bool EsPendienteSincronizar { get; set; }

    public bool TieneCambiosPendientes { get; set; }

    public Guid? PendingOperationId { get; set; }

    public RegistroActividadModel? RegistroOriginal { get; set; }
}