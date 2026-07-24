// Models/Actividades/ActividadRealizadaDetalleModel.cs
using System.Text.Json.Serialization;

namespace agaverosActividades.Models.Actividades
{
    public class ActividadRealizadaDetalleModel
    {
        [JsonPropertyName("intAGRActividadRealizadaKey")]
        public int IntAGRActividadRealizadaKey { get; set; }

        [JsonPropertyName("intAGRActividadLink")]
        public int IntAGRActividadLink { get; set; }

        [JsonPropertyName("intAGRSubActividadLink")]
        public int IntAGRSubActividadLink { get; set; }

        [JsonPropertyName("vchActividad")]
        public string VchActividad { get; set; }

        [JsonPropertyName("vchSubActividad")]
        public string VchSubActividad { get; set; }

        [JsonPropertyName("decValor")]
        public decimal DecValor { get; set; }

        [JsonPropertyName("decNoPlantas")]
        public decimal DecNoPlantas { get; set; }

        [JsonPropertyName("decNoPersonas")]
        public decimal DecNoPersonas { get; set; }

        [JsonPropertyName("vchUnidad")]
        public string VchUnidad { get; set; }

        [JsonPropertyName("vchObservaciones")]
        public string VchObservaciones { get; set; }

        [JsonPropertyName("vchHrsProductivas")]
        public string VchHrsProductivas { get; set; }

        [JsonPropertyName("vchHrsMuertas")]
        public string VchHrsMuertas { get; set; }

        [JsonPropertyName("decHorasPD")]
        public decimal DecHorasPD { get; set; }

        [JsonPropertyName("intGENProveedorLink")]
        public int? IntGENProveedorLink { get; set; }

        [JsonPropertyName("intGENOperadorMaquinariaLink")]
        public int? IntGENOperadorMaquinariaLink { get; set; }

        [JsonPropertyName("vchCodigoUnidad")]
        public string VchCodigoUnidad { get; set; }
    }
}