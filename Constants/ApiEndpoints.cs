namespace agaverosActividades.Constants;

public static class ApiEndpoints
{

    public const string BASE_API = AppConstants.ApiBaseUrl;

    public const string Usuarios = "api/usuarios";
    public const string Login = "Sesion/Login";
    public const string Logout = "api/auth/logout";
    public const string Sincronizar = "api/sincronizar";

    public const string ZonasCatalogo = "Zonas/Catalogo";
    public const string MunicipiosPorRegion = "Municipios/Region";
    public const string PrediosPorMunicipioZona = "Predios/MunicipioZona";
    public const string RegistroActividadCatalogo = "RegistroActividad/Catalogo";
    public const string RegistroActividadEliminar = "RegistroActividad/Eliminar";

    // ===== Formulario de Alta de Registro de Actividad (ABI Agave) =====
    public const string VehiculosActividad = "Vehiculos/Actividad";
    public const string TractoresCuadrillasCatalogo = "TractoresCuadrillas/Catalogo";
    public const string JefeCuadrillaCatalogo = "JefeCuadrilla/Catalogo";
    public const string ProveedoresCatalogo = "GENProveedor/Catalogo";
    public const string ActividadesSinCosecha = "Actividades/SinCosecha";
    public const string ClasificacionInsumoCatalogo = "ClasificacionInsumo/Catalogo";
    public const string EquiposCatalogo = "Equipos/Catalogo"; 
    public const string UnidadesActividad = "Unidades/Actividad";
    public const string SubactividadObtener = "Subactividad/Obtener";
    public const string SubactividadCatalogo = "Subactividad/Catalogo";
    public const string PreparacionesObtener = "Preparaciones/Obtener";
    public const string DetInsumoActividad = "DetInsumo/Actividad";
    public const string RegistroActividadAlta = "RegistroActividad/Alta";
    public const string RegistroActividadActividadRealizada = "RegistroActividad/ActividadRealizada";
    public const string RegistroActividadInsumoUtilizado = "RegistroActividad/InsumoUtilizado";
    public const string RegistroActividadMateriaPrimaUtilizada = "RegistroActividad/MateriaPrimaUtilizada";
    public const string RegistroActividadImplementoUtilizado = "RegistroActividad/ImplementoUtilizado";
    public const string RegistroActividadDetalleArchivo = "RegistroActividad/DetalleArchivo";
    public const string RegistroActividadObtenerActividades = "RegistroActividad/Obtener/Actividades";
    public const string RegistroActividadObtenerInsumos = "RegistroActividad/Obtener/Insumos";
    public const string RegistroActividadObtenerImplementos = "RegistroActividad/Obtener/Implementos";
    public const string RegistroActividadActualizar = "RegistroActividad/Actualizar";
    public const string PrediosCatalogo = "Predios/Catalogo";
    public const string OperadorMaquinariaCatalogo = "GENOperadorMaquinaria/Catalogo";
    public const string ActividadesCatalogo = "Actividades/Catalogo"; 
    public const string RegistroActividadAutorizar = "RegistroActividad/Autorizar";
    public const string RegistroActividadRechazar = "RegistroActividad/Rechazar";
    public const string RegistroActividadObtenerMateriasPrimas = "RegistroActividad/Obtener/MateriasPrimas";
}