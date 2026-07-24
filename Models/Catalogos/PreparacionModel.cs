namespace agaverosActividades.Models.Catalogos
{
    /// <summary>
    /// Preparación/insumo disponible dentro de una Clasificación. Al seleccionarla se refleja
    /// su unidad de costo y tipo de preparación en pantalla, y sirve para obtener el detalle
    /// de materia prima requerida (DetInsumoModel).
    /// </summary>
    public class PreparacionModel
    {
        public int IntAGRMastInsumoKey { get; set; }
        public int IntGENUnidadDosisLink { get; set; }
        public int IntGENUnidadCostoLink { get; set; }
        public int IntGENTipoPreparacionLink { get; set; }
        public int IntAGRClasificacionDeInsumoLink { get; set; }
        public string VchFolio { get; set; }
        public string VchClave { get; set; }
        public string VchDescripcion { get; set; }
        public string VchNombreComun { get; set; }
        public string VchNombreConClave { get; set; }
        public string VchUnidadDosis { get; set; }
        public string VchUnidadCosto { get; set; }
        public string VchClasificacion { get; set; }
        public string VchTipoPreparacion { get; set; }
        public decimal DecDosis { get; set; }
        public decimal MnyCosto { get; set; }
    }
}