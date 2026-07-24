using System;
using System.Collections.Generic;
using System.Text;

namespace agaverosActividades.Models.Catalogos
{
    public class PredioModel
    {
        public int IntGENPredioKey { get; set; }
        public int IntGENRegionLink { get; set; }
        public int IntGENMunicipioLink { get; set; }
        public int IntGENEstadoLink { get; set; }
        public int IntGENTipoSueloLink { get; set; }
        public string VchCodigo { get; set; } = string.Empty;
        public string VchAuxiliarCodigo { get; set; } = string.Empty;
        public string VchRegion { get; set; } = string.Empty;
        public string VchEstado { get; set; } = string.Empty;
        public string VchMunicipio { get; set; } = string.Empty;
        public decimal DecHAsEstimadas { get; set; }
        public decimal DecHAsPagadas { get; set; }
        public decimal DecTotalDeHAs { get; set; }
        public int? IntAnioDePlantacion { get; set; }
        public int IntTotalDePlantas { get; set; }
        public string VchLugar { get; set; } = string.Empty;
        public string VchNombre { get; set; } = string.Empty;
        public string VchTitular { get; set; } = string.Empty;
        public string VchResponsableDeZona { get; set; } = string.Empty;
        public string VchCuentaContable { get; set; } = string.Empty;
        public decimal DecPH { get; set; }
        public decimal MnySaldoInicial { get; set; }
        public DateTime? DtmFechaSaldoInicial { get; set; }
        public int IntGENEmpresaLink { get; set; }
        public string VchNombreEmpresa { get; set; } = string.Empty;
    }
}
