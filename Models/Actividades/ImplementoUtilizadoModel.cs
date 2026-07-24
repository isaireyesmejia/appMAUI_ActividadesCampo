// Models/Actividades/ImplementoUtilizadoModel.cs
namespace agaverosActividades.Models.Actividades
{
    /// <summary>
    /// DTO enviado al POST /RegistroActividad/ImplementoUtilizado.
    /// Usado tanto en Alta (IntMovimiento = Agregar) como en Edición
    /// (IntMovimiento = Agregar/Actualizar/Eliminar según corresponda).
    /// </summary>
    public class ImplementoUtilizadoModel
    {
        /// <summary>1 = Agregar, 2 = Actualizar, 3 = Eliminar (mismo criterio que ActividadRealizadaModel).</summary>
        public int IntMovimiento { get; set; }

        /// <summary>0 en Alta (nuevo). Key real del servidor cuando se actualiza/elimina uno existente.</summary>
        public int IntAGRImplementoUtilizadoKey { get; set; }

        public int IntAGRRegistroActividadLink { get; set; }

        public int IntGENImplementoLink { get; set; }

        public decimal DecCantidad { get; set; }

        public string VchNombre { get; set; }

        public string VchUsuario { get; set; }
    }
}