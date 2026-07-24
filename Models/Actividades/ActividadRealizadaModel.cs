namespace agaverosActividades.Models.Actividades;

public class ActividadRealizadaModel
{
    public int IntMovimiento { get; set; } // 1=Insert, 2=Update, 3=Delete
    public int IntAGRActividadRealizadaKey { get; set; }
    public int IntAGRRegistroActividadLink { get; set; }
    public int IntAGRActividadLink { get; set; }
    public int IntAGRSubActividadLink { get; set; }
    public decimal DecValor { get; set; }
    public decimal DecNoPlantas { get; set; }
    public decimal DecNoPersonas { get; set; }
    public string VchObservaciones { get; set; } = string.Empty;
    public string VchHrsProductivas { get; set; } = string.Empty;
    public string VchHrsMuertas { get; set; } = string.Empty;
    public decimal DecHorasPD { get; set; }
    public string VchNombreImagen { get; set; } = string.Empty;
    public string VchRutaArchivo { get; set; } = string.Empty;
    public string VchUsuario { get; set; } = string.Empty;
}