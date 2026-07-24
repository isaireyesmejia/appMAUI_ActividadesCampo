namespace agaverosActividades.Models.Catalogos
{
    /// <summary>
    /// Catálogo de actividades agrícolas (ej. "Riego", "Fertilización"), NO confundir con el
    /// modelo "Actividad" existente (ese es de otra funcionalidad con seguimiento GPS/estado).
    /// Unifica lo que en Xamarin eran ActividadesModel (online) y ActividadesSinCosechaModel (offline).
    /// </summary>
    public class ActividadAgricolaModel
    {
        public int IntAGRActividadKey { get; set; }
        public int IntAGREtapaKey { get; set; }
        public string VchClave { get; set; }
        public string VchDescripcion { get; set; }
        public bool BitAgroquimico { get; set; }
    }
}