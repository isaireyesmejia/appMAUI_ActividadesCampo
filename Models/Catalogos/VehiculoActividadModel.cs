namespace agaverosActividades.Models.Catalogos;

/// <summary>Catálogo de vehículos/unidades disponibles para una actividad.</summary>
public class VehiculoActividadModel
{
    public int IntGENUnidadParaActividadKey { get; set; }
    public string VchNoEconomico { get; set; } = string.Empty;
    public string VchPlacas { get; set; } = string.Empty;
    public string VchMarca { get; set; } = string.Empty;
    public string VchModelo { get; set; } = string.Empty;
    public int IntAnio { get; set; }
    public string VchNombreCompleto { get; set; } = string.Empty;
}