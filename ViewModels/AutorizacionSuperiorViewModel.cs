using agaverosActividades.Models;
using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using agaverosActividades.Services;
using agaverosActividades.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace agaverosActividades.ViewModels;

/// <summary>
/// ViewModel del listado de Autorización Superior. Mismo patrón de filtros
/// (Zona/Municipio/Predio + fecha) que RegistroActividadesViewModel, pero:
/// - Llama a ObtenerActividadesAsync con autoriza: "Superior" (solo registros pendientes
///   de autorización por un superior, igual que el Xamarin viejo).
/// - No tiene Agregar/Editar/Eliminar; en su lugar, VerDetalleCommand navega a la
///   pantalla de detalle donde se Autoriza o Rechaza el registro.
/// - No se mezclan pendientes de sincronizar (PendingOperations) porque esta pantalla
///   es de solo consulta/autorización, no de captura.
/// </summary>
public partial class AutorizacionSuperiorViewModel : ObservableObject
{
    private readonly IActividadService _actividadService;
    private readonly ISesionApp _sesionApp;
    private readonly ICatalogoCacheService _catalogoCacheService;

    public AutorizacionSuperiorViewModel(
        IActividadService actividadService,
        ISesionApp sesionApp,
        ICatalogoCacheService catalogoCacheService)
    {
        _actividadService = actividadService;
        _sesionApp = sesionApp;
        _catalogoCacheService = catalogoCacheService;
    }

    private List<ActividadListItem> _actividadesSinFiltrar = new();

    [ObservableProperty] private ObservableCollection<ZonaModel> zonas = new();
    [ObservableProperty] private ObservableCollection<MunicipioModel> municipios = new();
    [ObservableProperty] private ObservableCollection<PredioModel> predios = new();
    [ObservableProperty] private ObservableCollection<ActividadListItem> actividades = new();

    [ObservableProperty] private ZonaModel? zonaSeleccionada;
    [ObservableProperty] private MunicipioModel? municipioSeleccionado;
    [ObservableProperty] private PredioModel? predioSeleccionado;
    [ObservableProperty] private ActividadListItem? actividadSeleccionada;

    [ObservableProperty] private DateTime fechaBusqueda = DateTime.Today;
    [ObservableProperty] private bool filtroFechaActivo;

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
            var lista = await _catalogoCacheService.ObtenerAsync("Zonas", _actividadService.ObtenerZonasAsync);
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

        var idZona = ZonaSeleccionada.IntGENRegionKey;

        try
        {
            var lista = await _catalogoCacheService.ObtenerAsync(
                $"Municipios_Zona{idZona}",
                ct => _actividadService.ObtenerMunicipiosAsync(idZona, ct));
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

        var idZona = ZonaSeleccionada.IntGENRegionKey;
        var idMunicipio = MunicipioSeleccionado.IntGENMunicipioKey;

        try
        {
            var lista = await _catalogoCacheService.ObtenerAsync(
                $"Predios_Zona{idZona}_Mun{idMunicipio}",
                ct => _actividadService.ObtenerPrediosAsync(idZona, idMunicipio, ct));
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
            var listaServidor = await _actividadService.ObtenerActividadesAsync(
                ZonaSeleccionada.IntGENRegionKey, PredioSeleccionado.IntGENPredioKey, _sesionApp.Login,
                preparacionTerreno: false, autoriza: "Superior");

            var items = listaServidor.Select(r => new ActividadListItem
            {
                IntAGRRegistroActividadKey = r.IntAGRRegistroActividadKey,
                VchID = r.VchID,
                DtmFecha = r.DtmFecha,
                VchEstatus = r.VchEstatus,
                RegistroOriginal = r
            }).ToList();

            _actividadesSinFiltrar = items.OrderBy(a => a.DtmFecha).ToList();
            AplicarFiltroFecha();

            if (Actividades.Count == 0)
            {
                await Shell.Current.DisplayAlert("Sin resultados", "No hay registros pendientes de autorización con los criterios seleccionados.", "De acuerdo");
            }
        }
        catch (HttpRequestException)
        {
            await Shell.Current.DisplayAlert("Sin conexión", "No fue posible conectarse al servidor. Verifica tu conexión a internet.", "De acuerdo");
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
    private void SeleccionarActividad(ActividadListItem item)
    {
        ActividadSeleccionada = item;
    }

    [RelayCommand]
    private async Task VerDetalleAsync()
    {
        if (ActividadSeleccionada is null)
        {
            await Shell.Current.DisplayAlert("Advertencia", "Recuerde seleccionar el registro que desea revisar.", "Cerrar");
            return;
        }

        var parametros = new Dictionary<string, object>
        {
            { "registroActividad", ActividadSeleccionada.RegistroOriginal! }
        };

        await Shell.Current.GoToAsync(
            $"{nameof(AutorizacionSuperiorDetallePage)}?idActividad={ActividadSeleccionada.IntAGRRegistroActividadKey}",
            parametros);
    }

    [RelayCommand]
    private async Task CerrarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}