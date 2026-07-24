// Models/Actividades/ActualizarRegistroActividadModel.cs
namespace agaverosActividades.Models.Actividades
{
    public class ActualizarRegistroActividadModel
    {
        public int IntAGRRegistroActividadKey { get; set; }
        public int IntGENUnidadParaActividadLink { get; set; }
        public int IntGENPredioLink { get; set; }
        public int IntAGRTractoresCuadrillasLink { get; set; }
        public string VchID { get; set; }
        public DateTime DtmFecha { get; set; }
        public string VchJefeCuadrilla { get; set; }
        public string VchTipoCuadrilla { get; set; }
        public string VchObservaciones { get; set; }
        public string VchLogin { get; set; }
        public string VchEstatus { get; set; }
        public string VchHrsProductivasInicial { get; set; }
        public string VchHrsProductivasFinal { get; set; }
        public string VchHorometroInicial { get; set; }
        public string VchHorometroFinal { get; set; }
        public int? IntGENProveedorLink { get; set; }
        public int? IntGENOperadorMaquinariaLink { get; set; }
        public string VchCodigoUnidad { get; set; }
    }
}