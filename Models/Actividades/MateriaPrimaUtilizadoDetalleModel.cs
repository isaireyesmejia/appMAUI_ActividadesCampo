// Models/Actividades/MateriaPrimaUtilizadoDetalleModel.cs
using System.Text.Json.Serialization;

namespace agaverosActividades.Models.Actividades
{
    public class MateriaPrimaUtilizadoDetalleModel
    {
        [JsonPropertyName("intAGRMateriaPrimaUtilizadaKey")]
        public int IntAGRMateriaPrimaUtilizadaKey { get; set; }

        [JsonPropertyName("intAGRRegistroActividadLink")]
        public int IntAGRRegistroActividadLink { get; set; }

        [JsonPropertyName("vchMateriaPrima")]
        public string VchMateriaPrima { get; set; }

        [JsonPropertyName("vchUnidad")]
        public string VchUnidad { get; set; }

        [JsonPropertyName("decCantidadAplicado")]
        public decimal DecCantidadAplicado { get; set; }
    }
}