namespace agaverosActividades.Models.Catalogos
{
    /// <summary>
    /// Detalle de materia prima requerida por una Preparación/Insumo. No se usa en ningún Picker;
    /// se consulta al presionar "Agregar" para calcular las cantidades aplicadas de materia prima
    /// (DecRequerido * cantidad capturada de la preparación).
    /// Unifica lo que en Xamarin eran DetalleInsumoActividad (online) y DetInsumoModel (offline).
    /// </summary>
    public class DetInsumoModel
    {
        public int IntAGRMateriaPrimaLink { get; set; }
        public int IntAGRMastInsumoLink { get; set; }
        public string VchDescripcionMateriaPrima { get; set; }
        public string VchUnidadMateriaPrima { get; set; }
        public decimal DecRequerido { get; set; }
        public decimal DecCostoUnitarioMP { get; set; }
        public decimal DecCostoUnitarioMP2 { get; set; }
        public decimal DecImporte { get; set; }
    }
}