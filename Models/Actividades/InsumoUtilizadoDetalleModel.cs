// Models/Actividades/InsumoUtilizadoDetalleModel.cs
using System.Text.Json.Serialization;

namespace agaverosActividades.Models.Actividades
{
    public class InsumoUtilizadoDetalleModel
    {
        [JsonPropertyName("intAGRInsumoUtilizadoKey")]
        public int IntAGRInsumoUtilizadoKey { get; set; }

        [JsonPropertyName("intAGRMastInsumoLink")]
        public int IntAGRMastInsumoLink { get; set; }

        [JsonPropertyName("intAGRActividadLink")]
        public int IntAGRActividadLink { get; set; }

        [JsonPropertyName("vchActividad")]
        public string VchActividad { get; set; }

        [JsonPropertyName("vchInsumo")]
        public string VchInsumo { get; set; }

        [JsonPropertyName("decValor")]
        public decimal DecValor { get; set; }

        [JsonPropertyName("vchUnidad")]
        public string VchUnidad { get; set; }

        [JsonPropertyName("vchTipoPreparacion")]
        public string VchTipoPreparacion { get; set; }

        [JsonPropertyName("vchObservaciones")]
        public string VchObservaciones { get; set; }

        [JsonPropertyName("vchNoValeDeSalida")]
        public string VchNoValeDeSalida { get; set; }
    }
}