namespace agaverosActividades.Models.Catalogos
{
    /// <summary>Catálogo de proveedores (obligatorio solo cuando la cuadrilla es "Externo").</summary>
    public class ProveedorModel
    {
        public int IntGENProveedorKey { get; set; }
        public string VchCodigo { get; set; } = string.Empty;
        public string VchRazonSocial { get; set; } = string.Empty;
        public string VchNombreSocial { get; set; } = string.Empty;
        public DateTime DtmFechaCaptura { get; set; }
        public string VchUsuarioCaptura { get; set; } = string.Empty;
        public bool BitActivo { get; set; }
        public string VchUsuarioElimina { get; set; } = string.Empty;
        public DateTime? DtmFechaElimina { get; set; }
    }
}