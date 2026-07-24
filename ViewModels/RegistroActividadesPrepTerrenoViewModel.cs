using agaverosActividades.Models;
using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using agaverosActividades.Services;
using agaverosActividades.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace agaverosActividades.ViewModels;

public partial class RegistroActividadesPrepTerrenoViewModel : ObservableObject
{
    /// <summary>Clave fija de la actividad "Preparación de Terreno" (misma que en
    /// RegistroActividadPrepTerrenoFormViewModel.CLAVE_ACTIVIDAD_PREP_TERRENO).
    /// FIX #3: se usa para no mezclar pendientes de otras actividades en este listado.</summary>
    private const int CLAVE_ACTIVIDAD_PREP_TERRENO = 64;

    private readonly IActividadService _actividadService;
    private readonly ISesionApp _sesionApp;
    private readonly ILocalDataService _localDataService;
    private readonly ICatalogoCacheService _catalogoCacheService;

    private List<ActividadListItem> _actividadesSinFiltrar = new();

    public RegistroActividadesPrepTerrenoViewModel(
        IActividadService actividadService,
        ISesionApp sesionApp,
        ILocalDataService localDataService,
        ICatalogoCacheService catalogoCacheService)
    {
        _actividadService = actividadService;
        _sesionApp = sesionApp;
        _localDataService = localDataService;
        _catalogoCacheService = catalogoCacheService;
    }

    [ObservableProperty] private ObservableCollection<ZonaModel> zonas = new();
    [ObservableProperty] private ObservableCollection<MunicipioModel> municipios = new();
    [ObservableProperty] private ObservableCollection<PredioModel> predios = new();
    [ObservableProperty] private ObservableCollection<ActividadListItem> actividades = new();
    [ObservableProperty] private ZonaModel? zonaSeleccionada;
    [ObservableProperty] private MunicipioModel? municipioSeleccionado;
    [ObservableProperty] private PredioModel? predioSeleccionado;
    [ObservableProperty] private ActividadListItem? actividadSeleccionada;
    [ObservableProperty] private DateTime fechaBusqueda = DateTime.Today;
    [ObservableProperty] private bool filtroFechaActivo = true;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string usuario = string.Empty;
    [ObservableProperty] private string perfil = string.Empty;

    public async Task InicializarAsync()
    {
        Usuario = _sesionApp.NombreUsuario;
        Perfil = _sesionApp.Perfil;

        Municipios.Clear();
        Predios.Clear();
        Actividades.Clear();

        await CargarZonasAsync();
    }

    private async Task CargarZonasAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _catalogoCacheService.ObtenerAsync("Zonas", ct => _actividadService.ObtenerZonasAsync());
            Zonas = new ObservableCollection<ZonaModel>(lista);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudieron cargar las zonas.", "De acuerdo");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnZonaSeleccionadaChanged(ZonaModel? value)
    {
        _ = CargarMunicipiosAsync();
    }

    private async Task CargarMunicipiosAsync()
    {
        Predios.Clear();
        Actividades.Clear();
        MunicipioSeleccionado = null;

        if (ZonaSeleccionada is null) return;

        try
        {
            var lista = await _catalogoCacheService.ObtenerAsync(
                $"Municipios_Zona{ZonaSeleccionada.IntGENRegionKey}",
                ct => _actividadService.ObtenerMunicipiosAsync(ZonaSeleccionada.IntGENRegionKey));
            Municipios = new ObservableCollection<MunicipioModel>(lista);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudieron cargar los municipios.", "De acuerdo");
        }
    }

    partial void OnMunicipioSeleccionadoChanged(MunicipioModel? value)
    {
        _ = CargarPrediosAsync();
    }

    private async Task CargarPrediosAsync()
    {
        Actividades.Clear();
        PredioSeleccionado = null;

        if (ZonaSeleccionada is null || MunicipioSeleccionado is null) return;

        try
        {
            var lista = await _catalogoCacheService.ObtenerAsync(
                $"Predios_Zona{ZonaSeleccionada.IntGENRegionKey}_Mun{MunicipioSeleccionado.IntGENMunicipioKey}",
                ct => _actividadService.ObtenerPrediosAsync(ZonaSeleccionada.IntGENRegionKey, MunicipioSeleccionado.IntGENMunicipioKey));
            Predios = new ObservableCollection<PredioModel>(lista);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudieron cargar los predios.", "De acuerdo");
        }
    }

    [RelayCommand]
    private async Task MostrarAsync()
    {
        if (ZonaSeleccionada is null || MunicipioSeleccionado is null || PredioSeleccionado is null)
        {
            await Shell.Current.DisplayAlert("Advertencia", "Favor de seleccionar zona, municipio y predio.", "Cerrar");
            return;
        }

        IsLoading = true;
        try
        {
            List<RegistroActividadModel> listaServidor;
            bool sinConexion = false;

            try
            {
                // preparacionTerreno: true -> única diferencia respecto a RegistroActividadesViewModel
                listaServidor = await _actividadService.ObtenerActividadesAsync(
                    ZonaSeleccionada.IntGENRegionKey, PredioSeleccionado.IntGENPredioKey, _sesionApp.Login, preparacionTerreno: true);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                listaServidor = new List<RegistroActividadModel>();
                sinConexion = true;
            }

            var items = listaServidor.Select(r => new ActividadListItem
            {
                IntAGRRegistroActividadKey = r.IntAGRRegistroActividadKey,
                VchID = r.VchID,
                DtmFecha = r.DtmFecha,
                VchEstatus = r.VchEstatus,
                RegistroOriginal = r
            }).ToList();

            await MezclarPendientesAsync(items);

            _actividadesSinFiltrar = items.OrderBy(a => a.DtmFecha).ToList();
            AplicarFiltroFecha();

            if (sinConexion)
            {
                await Shell.Current.DisplayAlert(
                    "Sin conexión",
                    "No fue posible conectarse al servidor. Se muestran únicamente los registros guardados en este dispositivo pendientes de sincronizar.",
                    "De acuerdo");
            }
            else if (Actividades.Count == 0)
            {
                await Shell.Current.DisplayAlert("Sin resultados", "No se encontraron registros con los criterios seleccionados.", "De acuerdo");
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "No responde el servidor.", "De acuerdo");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MezclarPendientesAsync(List<ActividadListItem> items)
    {
        List<PendingOperation> pendientes;
        try
        {
            pendientes = await _localDataService.ObtenerPendientesAsync();
        }
        catch (Exception)
        {
            return;
        }

        foreach (var operacion in pendientes)
        {
            GuardarRegistroActividadPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<GuardarRegistroActividadPayload>(operacion.PayloadJson);
            }
            catch (Exception)
            {
                continue;
            }

            if (payload is null) continue;

            // FIX #3: antes solo se filtraba por predio + login, así que una captura pendiente
            // de OTRA actividad (hecha desde el formulario general) para el mismo predio/usuario
            // se colaba en este listado de Prep. Terreno. Se agrega el filtro por actividad.
            if (payload.ActividadRealizada?.IntAGRActividadLink != CLAVE_ACTIVIDAD_PREP_TERRENO) continue;

            if (payload.Alta != null)
            {
                if (payload.Alta.IntGENPredioLink != PredioSeleccionado!.IntGENPredioKey) continue;
                if (!string.Equals(payload.Alta.VchLogin, _sesionApp.Login, StringComparison.OrdinalIgnoreCase)) continue;

                items.Add(new ActividadListItem
                {
                    IntAGRRegistroActividadKey = null,
                    VchID = payload.Alta.VchID,
                    DtmFecha = payload.Alta.DtmFecha,
                    VchEstatus = "Sin sincronizar",
                    EsPendienteSincronizar = true,
                    PendingOperationId = operacion.Id
                });
            }
            else if (payload.Actualizacion != null)
            {
                var existente = items.FirstOrDefault(i => i.IntAGRRegistroActividadKey == payload.ActividadIdEdicion);
                if (existente != null)
                    existente.TieneCambiosPendientes = true;
            }
        }
    }

    partial void OnFechaBusquedaChanged(DateTime value)
    {
        FiltroFechaActivo = true;
        AplicarFiltroFecha();
    }

    partial void OnFiltroFechaActivoChanged(bool value)
    {
        AplicarFiltroFecha();
    }

    private void AplicarFiltroFecha()
    {
        if (!FiltroFechaActivo)
        {
            Actividades = new ObservableCollection<ActividadListItem>(_actividadesSinFiltrar);
            return;
        }

        var filtrados = _actividadesSinFiltrar
            .Where(a => a.DtmFecha.Date == FechaBusqueda.Date)
            .ToList();

        Actividades = new ObservableCollection<ActividadListItem>(filtrados);
    }

    [RelayCommand]
    private void LimpiarFechaBusqueda()
    {
        FiltroFechaActivo = false;
    }

    [RelayCommand]
    private async Task AgregarAsync()
    {
        if (ZonaSeleccionada is null || MunicipioSeleccionado is null || PredioSeleccionado is null)
        {
            await Shell.Current.DisplayAlert("Advertencia", "Recuerde seleccionar la zona y el predio.", "Cerrar");
            return;
        }

        // Única diferencia respecto a RegistroActividadesViewModel: navega al form de Prep. Terreno.
        await Shell.Current.GoToAsync(
            $"{nameof(RegistroActividadPrepTerrenoFormPage)}?idPredio={PredioSeleccionado.IntGENPredioKey}&idZona={ZonaSeleccionada.IntGENRegionKey}&idMunicipio={MunicipioSeleccionado.IntGENMunicipioKey}");
    }

    [RelayCommand]
    private async Task EditarAsync()
    {
        if (ActividadSeleccionada is null)
        {
            await Shell.Current.DisplayAlert("Advertencia", "Recuerde seleccionar el registro que desea editar.", "Cerrar");
            return;
        }

        if (ActividadSeleccionada.EsPendienteSincronizar)
        {
            await Shell.Current.DisplayAlert(
                "Aún no disponible",
                "Este registro todavía no se ha sincronizado con el servidor. Espera a que sincronice para poder editarlo.",
                "De acuerdo");
            return;
        }

        if (ActividadSeleccionada.VchEstatus == "Validado")
        {
            await Shell.Current.DisplayAlert("Advertencia", "Registro en proceso de autorización, no puede ser editado.", "Confirmar");
            return;
        }

        var parametros = new Dictionary<string, object>
        {
            { "registroActividad", ActividadSeleccionada.RegistroOriginal! }
        };

        await Shell.Current.GoToAsync(
            $"{nameof(RegistroActividadPrepTerrenoFormPage)}?idActividad={ActividadSeleccionada.IntAGRRegistroActividadKey}",
            parametros);
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (ActividadSeleccionada is null)
        {
            await Shell.Current.DisplayAlert("Advertencia", "Recuerde seleccionar el registro que desea eliminar.", "Cerrar");
            return;
        }

        if (ActividadSeleccionada.EsPendienteSincronizar)
        {
            await Shell.Current.DisplayAlert(
                "Aún no disponible",
                "Este registro todavía no se ha sincronizado con el servidor. Espera a que sincronice para poder eliminarlo.",
                "De acuerdo");
            return;
        }

        if (ActividadSeleccionada.VchEstatus == "Validado")
        {
            await Shell.Current.DisplayAlert("Advertencia", "Registro en proceso de autorización, no puede ser eliminado.", "Confirmar");
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(
                "Sin conexión",
                "Necesitas conexión a internet para eliminar un registro.",
                "De acuerdo");
            return;
        }

        bool confirmar = await Shell.Current.DisplayAlert("Eliminar Registro", "¿Deseas eliminar este registro?", "Confirmar", "Cancelar");
        if (!confirmar) return;

        var idEliminado = ActividadSeleccionada.IntAGRRegistroActividadKey!.Value;

        try
        {
            await _actividadService.EliminarActividadAsync(idEliminado, _sesionApp.Login);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EliminarAsync - excepción de red: {ex.Message}");
        }

        await MostrarAsync();

        bool sigueExistiendo = Actividades.Any(a => a.IntAGRRegistroActividadKey == idEliminado);

        if (!sigueExistiendo)
        {
            await Shell.Current.DisplayAlert("Éxito", "Registro eliminado exitosamente.", "Confirmar");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo confirmar la eliminación del registro.", "De acuerdo");
        }

        ActividadSeleccionada = null;
    }

    [RelayCommand]
    private async Task CerrarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void SeleccionarActividad(ActividadListItem item)
    {
        ActividadSeleccionada = item;
    }
}