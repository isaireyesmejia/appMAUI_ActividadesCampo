// Models/CatalogoCache.cs
using SQLite;

namespace agaverosActividades.Models;

/// <summary>
/// Caché genérica de catálogos de solo lectura (Predios, Vehículos, Tractores/Cuadrillas,
/// Jefes de Cuadrilla, Actividades, Preparaciones, Equipos, Unidades, etc.) para poder
/// llenar los combos del formulario aunque no haya conexión. Un renglón por catálogo,
/// identificado por Nombre, con la lista completa serializada en JSON.
/// </summary>
[Table("CatalogoCache")]
public class CatalogoCache
{
    [PrimaryKey]
    public string Nombre { get; set; } = string.Empty;

    public string JsonData { get; set; } = string.Empty;

    public DateTime FechaActualizacion { get; set; }
}