// Models/Actividades/MateriaPrimaUtilizadoModel.cs
namespace agaverosActividades.Models.Actividades
{
    /// <summary>
    /// Representa una materia prima "explotada" a partir de una preparación/insumo agregado.
    /// Se calcula en el cliente (cantidad requerida × ración aplicada) y se envía al backend
    /// tal como lo hace el portal WebForms en btnGuardar_Click / MateriaPrimaUtilizada.
    /// </summary>
    public class MateriaPrimaUtilizadoModel
    {
        public enum MovimientoMP
        {
            Nada = 0,
            Agregar = 1,
            Actualizar = 2,
            Eliminar = 3
        }

        public MovimientoMP IntMovimiento { get; set; } = MovimientoMP.Agregar;
        public int IntAGRMateriaPrimaUtilizadaKey { get; set; }
        public int IntAGRRegistroActividadLink { get; set; }
        public int IntAGRMateriaPrimaLink { get; set; }
        public int IntAGRMastInsumoLink { get; set; }
        public int IntAGRActividadLink { get; set; }
        public decimal DecValor { get; set; }
        public string VchObservaciones { get; set; } = string.Empty;
        public string VchUsuario { get; set; } = string.Empty;
    }
}