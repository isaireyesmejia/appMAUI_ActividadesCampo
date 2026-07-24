// Models/Actividades/InsumoUtilizadoModel.cs
namespace agaverosActividades.Models.Actividades;

public class InsumoUtilizadoModel
{
    public int IntMovimiento { get; set; } // 1=Insert, 2=Update, 3=Delete
    public int IntAGRInsumoUtilizadoKey { get; set; }
    public int IntAGRRegistroActividadLink { get; set; }
    public int IntAGRMastInsumoLink { get; set; }
    public int IntAGRActividadLink { get; set; }
    public decimal DecValor { get; set; }
    public string VchObservaciones { get; set; } = string.Empty;
    public string VchNoValeDeSalida { get; set; } = string.Empty;
    public string VchUsuario { get; set; } = string.Empty;
}