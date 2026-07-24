namespace agaverosActividades.Models.Catalogos
{
    /// <summary>
    /// Catálogo de subactividades, dependiente de la Actividad seleccionada (IntAGRActividadLink).
    /// Al seleccionarla se refleja su unidad de medida en pantalla.
    /// Unifica lo que en Xamarin eran SubactividadActividadesModel (online) y SubactividadModel (offline).
    /// </summary>
    public class SubactividadModel
    {
        public int IntAGRSubActividadKey { get; set; }
        public int IntAGRActividadLink { get; set; }
        public string VchSubActividad { get; set; }
        public string VchUnidadDeMedida { get; set; }
        public bool BitHabilitado { get; set; } = true;
    }
}