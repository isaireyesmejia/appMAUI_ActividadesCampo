// Models/Catalogos/OperadorMaquinariaModel.cs
using System.Text.Json.Serialization;

namespace agaverosActividades.Models.Catalogos;

public class OperadorMaquinariaModel
{
    [JsonPropertyName("intGENOperadorMaquinariaKey")]
    public int IntGENOperadorMaquinariaKey { get; set; }

    [JsonPropertyName("vchNombre")]
    public string VchNombre { get; set; } = string.Empty;

    [JsonPropertyName("intGENProveedorLink")]
    public int IntGENProveedorLink { get; set; }
}