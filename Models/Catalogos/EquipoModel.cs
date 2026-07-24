namespace agaverosActividades.Models.Catalogos
{
    /// <summary>
    /// Catálogo de Equipo/Implemento. Unifica lo que en Xamarin eran dos modelos distintos
    /// (EquipoModel y EquipoCompletoModel) según el origen de los datos; el formulario en MAUI
    /// solo necesita un tipo para el Picker de Equipo.
    /// </summary>
    public class EquipoModel
    {
        public int IntGENImplementoKey { get; set; }
        public string VchNombre { get; set; }
        public string VchNombreConCodigo { get; set; }
    }
}