using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace agaverosActividades.Models.Actividades
{
    public class RegistroActividadModel
    {
        public string VchID { get; set; }
        public int IntAGRRegistroActividadKey { get; set; }
        public DateTime DtmFecha { get; set; }
        public DateTime DtmFechaCaptura { get; set; }
        public string VchEstatus { get; set; }
        public string VchEstatusRegistro { get; set; }
        public string VchObservacionActividad { get; set; }
        public string VchTipoCuadrilla { get; set; }
        public string VchJefeCuadrilla { get; set; }
        public string VchHorometroInicial { get; set; }
        public string VchHorometroFinal { get; set; }
        public string VchHrsProductivasInicial { get; set; }
        public string VchHrsProductivasFinal { get; set; }
        public decimal DecHorasExtras { get; set; }
        public int IntGENProveedorLink { get; set; }
        public int IntGENUnidadParaActividadLink { get; set; }
        public int IntGENPredioLink { get; set; }
        public int IntAGRTractoresCuadrillasLink { get; set; }
        public string VchUsuarioCaptura { get; set; }
        public string VchNombreImagen { get; set; }
        public string VchObservacionRechazo { get; set; }
        public bool BitRechazado { get; set; }
        public bool BitAutorizaSuperior { get; set; }
        public bool BitAutorizaControlInterno { get; set; }
        [JsonPropertyName("intGENOperadorMaquinariaLink")]
        public int? IntGENOperadorMaquinariaLink { get; set; }

        [JsonPropertyName("vchCodigoUnidad")]
        public string VchCodigoUnidad { get; set; }
    }
}