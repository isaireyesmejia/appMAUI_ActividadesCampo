namespace agaverosActividades.Models.Actividades;

public class AltaRegistroActividadModel
{
    public int IntGENUnidadParaActividadLink { get; set; }
    public int IntGENPredioLink { get; set; }
    public int IntAGRTractoresCuadrillasLink { get; set; }
    public string VchID { get; set; } = string.Empty;
    public DateTime DtmFecha { get; set; }
    public string VchJefeCuadrilla { get; set; } = string.Empty;
    public string VchTipoCuadrilla { get; set; } = string.Empty;
    public string VchObservaciones { get; set; } = string.Empty;
    public string VchLogin { get; set; } = string.Empty;
    public decimal? DecHorasExtras { get; set; }
    public string VchEstatus { get; set; } = string.Empty;
    public string VchHrsProductivasInicial { get; set; } = string.Empty;
    public string VchHrsProductivasFinal { get; set; } = string.Empty;
    public string VchHorometroInicial { get; set; } = string.Empty;
    public string VchHorometroFinal { get; set; } = string.Empty;
    public int? IntGENProveedorLink { get; set; }
    public int? IntGENOperadorMaquinariaLink { get; set; }
    public string VchCodigoUnidad { get; set; } = string.Empty;
}