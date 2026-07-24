// Services/ActividadService.cs
using agaverosActividades.Constants;
using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using System.Net.Http.Json;

namespace agaverosActividades.Services;

public interface IActividadService
{
    // ===== Listado RegistroActividadesPage =====
    Task<List<ZonaModel>> ObtenerZonasAsync(CancellationToken cancellationToken = default);
    Task<List<MunicipioModel>> ObtenerMunicipiosAsync(int idZona, CancellationToken cancellationToken = default);
    Task<List<PredioModel>> ObtenerPrediosAsync(int idZona, int idMunicipio, CancellationToken cancellationToken = default);
    Task<bool> EliminarActividadAsync(int idActividad, string login);

    // ===== Catálogos del formulario de Alta/Edición de Registro de Actividad =====
    Task<List<VehiculoActividadModel>> ObtenerVehiculosAsync(CancellationToken cancellationToken = default);
    Task<List<TractorCuadrillaModel>> ObtenerTractoresCuadrillasAsync(CancellationToken cancellationToken = default);
    Task<List<JefeCuadrillaModel>> ObtenerJefesCuadrillaAsync(CancellationToken cancellationToken = default);
    Task<List<ProveedorModel>> ObtenerProveedoresAsync(CancellationToken cancellationToken = default);
    Task<List<PredioModel>> ObtenerPrediosCatalogoAsync(CancellationToken cancellationToken = default);
    Task<List<ActividadAgricolaModel>> ObtenerActividadesSinCosechaAsync(CancellationToken cancellationToken = default);
    Task<List<ClasificacionInsumoModel>> ObtenerClasificacionesAsync(CancellationToken cancellationToken = default);
    Task<List<EquipoModel>> ObtenerEquiposAsync(CancellationToken cancellationToken = default);
    Task<List<UnidadEquipoModel>> ObtenerUnidadesActividadAsync(CancellationToken cancellationToken = default);
    Task<List<SubactividadModel>> ObtenerSubactividadesAsync(int etapaKey, int actividadKey, CancellationToken cancellationToken = default);
    Task<List<PreparacionModel>> ObtenerPreparacionesAsync(CancellationToken cancellationToken = default);
    Task<List<DetInsumoModel>> ObtenerDetalleInsumoAsync(int mastInsumoKey, CancellationToken cancellationToken = default);

    // ===== Guardado del formulario de Alta =====
    Task<AltaRegistroActividadReturnModel> AltaRegistroActividadAsync(AltaRegistroActividadModel modelo);
    Task<ActividadRealizadaReturnModel> ActividadRealizadaAsync(ActividadRealizadaModel modelo);
    Task InsumoUtilizadoAsync(InsumoUtilizadoModel modelo);
    Task ImplementoUtilizadoAsync(ImplementoUtilizadoModel modelo);
    Task MateriaPrimaUtilizadaAsync(MateriaPrimaUtilizadoModel modelo);
    Task<string> SubirImagenAsync(string filePath, string fileName);

    // ===== Detalle y guardado del formulario de Edición =====
    Task<ActividadRealizadaDetalleModel?> ObtenerActividadRealizadaAsync(int idRegistroActividad, bool preparacionTerreno = false, CancellationToken cancellationToken = default);
    Task<List<InsumoUtilizadoDetalleModel>> ObtenerInsumosUtilizadosAsync(int idRegistroActividad, CancellationToken cancellationToken = default);
    Task<List<ImplementoUtilizadoDetalleModel>> ObtenerImplementosUtilizadosAsync(int idRegistroActividad, CancellationToken cancellationToken = default);
    Task ActualizarRegistroActividadAsync(ActualizarRegistroActividadModel modelo);
    Task<List<OperadorMaquinariaModel>> ObtenerOperadoresMaquinariaAsync(int idProveedor, CancellationToken cancellationToken = default);
    // Interfaz
    Task<List<RegistroActividadModel>> ObtenerActividadesAsync(int idZona, int idPredio, string login, bool preparacionTerreno = false, string autoriza = "", CancellationToken cancellationToken = default);
    Task<List<ActividadAgricolaModel>> ObtenerActividadesCatalogoAsync(CancellationToken cancellationToken = default);
    // ===== Autorización Superior =====
    Task<List<MateriaPrimaUtilizadoDetalleModel>> ObtenerMateriasPrimasUtilizadasAsync(int idRegistroActividad, CancellationToken cancellationToken = default);
    Task AutorizarRegistroActividadAsync(AutorizarRegistroActividadModel modelo);
    Task RechazarRegistroActividadAsync(RechazarRegistroActividadModel modelo);
}

public class ActividadService : IActividadService
{
    private readonly HttpClient _httpClient;

    public ActividadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ===== Listado RegistroActividadesPage =====

    public async Task<List<ZonaModel>> ObtenerZonasAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.ZonasCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ZonaModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<MunicipioModel>> ObtenerMunicipiosAsync(int idZona, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{ApiEndpoints.MunicipiosPorRegion}?region={idZona}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MunicipioModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<PredioModel>> ObtenerPrediosAsync(int idZona, int idMunicipio, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{ApiEndpoints.PrediosPorMunicipioZona}?region={idZona}&municipio={idMunicipio}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PredioModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<bool> EliminarActividadAsync(int idActividad, string login)
    {
        var body = new EliminarRegistroActividadModel { IntAGRRegistroActividadLink = idActividad, VchLogin = login };
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadEliminar, body);
        return response.IsSuccessStatusCode;
    }

    // ===== Catálogos del formulario de Alta/Edición =====

    public async Task<List<VehiculoActividadModel>> ObtenerVehiculosAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.VehiculosActividad, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<VehiculoActividadModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<TractorCuadrillaModel>> ObtenerTractoresCuadrillasAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.TractoresCuadrillasCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TractorCuadrillaModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<JefeCuadrillaModel>> ObtenerJefesCuadrillaAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.JefeCuadrillaCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        var lista = await response.Content.ReadFromJsonAsync<List<JefeCuadrillaModel>>(JsonOptions.Default, cancellationToken) ?? new();
        return lista.OrderBy(j => j.VchNombre).ToList();
    }

    public async Task<List<ProveedorModel>> ObtenerProveedoresAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.ProveedoresCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProveedorModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<PredioModel>> ObtenerPrediosCatalogoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.PrediosCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PredioModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<ActividadAgricolaModel>> ObtenerActividadesSinCosechaAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.ActividadesSinCosecha, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ActividadAgricolaModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<ClasificacionInsumoModel>> ObtenerClasificacionesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.ClasificacionInsumoCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ClasificacionInsumoModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<EquipoModel>> ObtenerEquiposAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.EquiposCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EquipoModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<UnidadEquipoModel>> ObtenerUnidadesActividadAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.UnidadesActividad, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UnidadEquipoModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<SubactividadModel>> ObtenerSubactividadesAsync(int etapaKey, int actividadKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{ApiEndpoints.SubactividadObtener}?etapa={etapaKey}&actividad={actividadKey}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SubactividadModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<PreparacionModel>> ObtenerPreparacionesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.PreparacionesObtener, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PreparacionModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<DetInsumoModel>> ObtenerDetalleInsumoAsync(int mastInsumoKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{ApiEndpoints.DetInsumoActividad}?id={mastInsumoKey}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DetInsumoModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    // ===== Guardado del formulario de Alta =====

    public async Task<AltaRegistroActividadReturnModel> AltaRegistroActividadAsync(AltaRegistroActividadModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadAlta, modelo);

        if (!response.IsSuccessStatusCode)
        {
            var mensajeError = await response.Content.ReadAsStringAsync();
            // PASO 1 (offline): se agrega response.StatusCode como tercer parámetro.
            // Esto permite que quien capture la excepción distinga:
            //   ex.StatusCode == null  -> no hubo respuesta del servidor (sin red / timeout)
            //   ex.StatusCode != null  -> el servidor respondió con un error de negocio
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(mensajeError)
                    ? $"Error {(int)response.StatusCode} al dar de alta el registro."
                    : mensajeError,
                null,
                response.StatusCode);
        }

        var lista = await response.Content.ReadFromJsonAsync<List<AltaRegistroActividadReturnModel>>(JsonOptions.Default);
        return lista?.FirstOrDefault() ?? new AltaRegistroActividadReturnModel();
    }

    public async Task<ActividadRealizadaReturnModel> ActividadRealizadaAsync(ActividadRealizadaModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadActividadRealizada, modelo);

        if (!response.IsSuccessStatusCode)
        {
            var mensajeError = await response.Content.ReadAsStringAsync();
            // PASO 1 (offline): mismo fix que en AltaRegistroActividadAsync.
            throw new HttpRequestException(mensajeError, null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<ActividadRealizadaReturnModel>(JsonOptions.Default)
            ?? new ActividadRealizadaReturnModel();
    }

    public async Task InsumoUtilizadoAsync(InsumoUtilizadoModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadInsumoUtilizado, modelo);
        response.EnsureSuccessStatusCode();
        // Nota: el backend no regresa RetVal/ErrorMessage (endpoint void),
        // así que solo podemos confirmar que la petición HTTP llegó bien,
        // no que el SP haya insertado sin errores de negocio.
    }

    public async Task<List<OperadorMaquinariaModel>> ObtenerOperadoresMaquinariaAsync(int idProveedor, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{ApiEndpoints.OperadorMaquinariaCatalogo}?intGENProveedorKey={idProveedor}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<OperadorMaquinariaModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task ImplementoUtilizadoAsync(ImplementoUtilizadoModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadImplementoUtilizado, modelo);
        response.EnsureSuccessStatusCode();
        // Nota: mismo caso que InsumoUtilizado — endpoint void, sin RetVal/ErrorMessage.
    }

    public async Task MateriaPrimaUtilizadaAsync(MateriaPrimaUtilizadoModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadMateriaPrimaUtilizada, modelo);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> SubirImagenAsync(string filePath, string fileName)
    {
        using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();

        var nombreDestino = $"{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(fileName)}";
        content.Add(new StreamContent(stream), "file", nombreDestino);

        var response = await _httpClient.PostAsync(ApiEndpoints.RegistroActividadDetalleArchivo, content);
        response.EnsureSuccessStatusCode();

        var nombreArchivo = await response.Content.ReadFromJsonAsync<string>(JsonOptions.Default) ?? nombreDestino;

        return $"{_httpClient.BaseAddress}ArchivosActividadRealizada/{nombreArchivo}";
    }

    // ===== Detalle y guardado del formulario de Edición =====

    public async Task<ActividadRealizadaDetalleModel?> ObtenerActividadRealizadaAsync(int idRegistroActividad, bool preparacionTerreno = false, CancellationToken cancellationToken = default)
    {
        var lista = await _httpClient.GetFromJsonAsync<List<ActividadRealizadaDetalleModel>>(
            $"{ApiEndpoints.RegistroActividadObtenerActividades}?id={idRegistroActividad}&bitPreparacionTerreno={preparacionTerreno.ToString().ToLower()}",
            JsonOptions.Default,
            cancellationToken);

        return lista?.FirstOrDefault();
    }

    public async Task<List<InsumoUtilizadoDetalleModel>> ObtenerInsumosUtilizadosAsync(int idRegistroActividad, CancellationToken cancellationToken = default)
    {
        var lista = await _httpClient.GetFromJsonAsync<List<InsumoUtilizadoDetalleModel>>(
            $"{ApiEndpoints.RegistroActividadObtenerInsumos}?id={idRegistroActividad}",
            JsonOptions.Default,
            cancellationToken);

        return lista ?? new List<InsumoUtilizadoDetalleModel>();
    }

    public async Task<List<ImplementoUtilizadoDetalleModel>> ObtenerImplementosUtilizadosAsync(int idRegistroActividad, CancellationToken cancellationToken = default)
    {
        var lista = await _httpClient.GetFromJsonAsync<List<ImplementoUtilizadoDetalleModel>>(
            $"{ApiEndpoints.RegistroActividadObtenerImplementos}?id={idRegistroActividad}",
            JsonOptions.Default,
            cancellationToken);

        return lista ?? new List<ImplementoUtilizadoDetalleModel>();
    }

    public async Task ActualizarRegistroActividadAsync(ActualizarRegistroActividadModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadActualizar, modelo);
        response.EnsureSuccessStatusCode();
    }

    // Implementación
    public async Task<List<RegistroActividadModel>> ObtenerActividadesAsync(int idZona, int idPredio, string login, bool preparacionTerreno = false, string autoriza = "", CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{ApiEndpoints.RegistroActividadCatalogo}?zona={idZona}&predio={idPredio}&login={login}&autoriza={autoriza}&preparacionTerreno={preparacionTerreno.ToString().ToLower()}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RegistroActividadModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<ActividadAgricolaModel>> ObtenerActividadesCatalogoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.ActividadesCatalogo, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ActividadAgricolaModel>>(JsonOptions.Default, cancellationToken) ?? new();
    }

    public async Task<List<MateriaPrimaUtilizadoDetalleModel>> ObtenerMateriasPrimasUtilizadasAsync(int idRegistroActividad, CancellationToken cancellationToken = default)
    {
        var lista = await _httpClient.GetFromJsonAsync<List<MateriaPrimaUtilizadoDetalleModel>>(
            $"{ApiEndpoints.RegistroActividadObtenerMateriasPrimas}?id={idRegistroActividad}",
            JsonOptions.Default,
            cancellationToken);

        return lista ?? new List<MateriaPrimaUtilizadoDetalleModel>();
    }

    public async Task AutorizarRegistroActividadAsync(AutorizarRegistroActividadModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadAutorizar, modelo);
        response.EnsureSuccessStatusCode();
    }

    public async Task RechazarRegistroActividadAsync(RechazarRegistroActividadModel modelo)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.RegistroActividadRechazar, modelo);
        response.EnsureSuccessStatusCode();
    }
}