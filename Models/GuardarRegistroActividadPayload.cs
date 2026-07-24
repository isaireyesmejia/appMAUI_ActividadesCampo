// Models/GuardarRegistroActividadPayload.cs
using agaverosActividades.Models.Actividades;

namespace agaverosActividades.Models;

/// <summary>
/// Todo lo necesario para reproducir un Guardar() completo (Alta o Edición)
/// cuando haya conexión. El ViewModel arma este objeto con los mismos datos
/// que ya usa hoy en GuardarAltaAsync/GuardarEdicionAsync, y en vez de llamar
/// directo al servicio, lo pasa a ILocalDataService.GuardarAsync.
/// </summary>
public class GuardarRegistroActividadPayload
{
    /// <summary>Texto corto para mostrar en la lista de pendientes (ej. "Riego - Predio Los Álamos").</summary>
    public string Descripcion { get; set; } = string.Empty;

    public AltaRegistroActividadModel? Alta { get; set; }               // null si es edición
    public ActualizarRegistroActividadModel? Actualizacion { get; set; } // null si es alta

    /// <summary>Solo se usa en modo Edición (ActividadId ya existe en servidor).</summary>
    public int ActividadIdEdicion { get; set; }

    /// <summary>Solo se usa en modo Edición: key real de AGRActividadRealizada original.</summary>
    public int ActividadRealizadaKeyOriginal { get; set; }

    public ActividadRealizadaModel ActividadRealizada { get; set; } = null!;
    public List<InsumoUtilizadoModel> Insumos { get; set; } = new();
    public List<ImplementoUtilizadoModel> Implementos { get; set; } = new();
    public List<MateriaPrimaUtilizadoModel> MateriaPrima { get; set; } = new();

    /// <summary>Ruta local del archivo de imagen (si el usuario tomó/eligió una foto nueva).</summary>
    public string? ImagenPathLocal { get; set; }
    public string? ImagenNombre { get; set; }

    /// <summary>Modo edición: si el usuario dejó la imagen original sin cambios, va la URL remota
    /// tal cual para reenviarla sin volver a subir.</summary>
    public string? ImagenUrlRemotaSinCambios { get; set; }
}