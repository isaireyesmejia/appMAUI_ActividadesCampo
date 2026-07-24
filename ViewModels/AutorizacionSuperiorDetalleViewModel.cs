using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using agaverosActividades.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace agaverosActividades.ViewModels
{
    /// <summary>
    /// ViewModel de AutorizacionSuperiorDetallePage. Pantalla de SOLO LECTURA
    /// (encabezado, Actividad Realizada, Materia Prima, Insumos, Implementos) con
    /// dos acciones finales: Autorizar o Rechazar el registro.
    ///
    /// Patrón de navegación: igual que RegistroActividadPrepTerrenoFormViewModel,
    /// recibe "idActividad" por query string y el objeto completo "registroActividad"
    /// (RegistroActividadModel) via ApplyQueryAttributes, para no tener que ir a pedirlo
    /// de nuevo al servidor.
    ///
    /// El popup de "Motivo de Rechazo" es un overlay dentro de la misma página
    /// (MostrarPopupRechazo + MotivoRechazo), no una página modal aparte: mismo
    /// patrón que el overlay de "Cargando" ya usado en el resto de la app, evita
    /// una navegación extra y mantiene todo el flujo en un solo lugar.
    /// </summary>
    [QueryProperty(nameof(ActividadId), "idActividad")]
    public partial class AutorizacionSuperiorDetalleViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IActividadService _actividadService;
        private readonly ISesionApp _sesionApp;
        private readonly ICatalogoCacheService _catalogoCacheService;

        private RegistroActividadModel? _registroActividadOriginal;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("registroActividad", out var value) && value is RegistroActividadModel modelo)
                _registroActividadOriginal = modelo;
        }

        public AutorizacionSuperiorDetalleViewModel(
            IActividadService actividadService,
            ISesionApp sesionApp,
            ICatalogoCacheService catalogoCacheService)
        {
            _actividadService = actividadService;
            _sesionApp = sesionApp;
            _catalogoCacheService = catalogoCacheService;
        }

        // ===================== PARÁMETROS DE NAVEGACIÓN =====================

        [ObservableProperty] private int actividadId;

        // ===================== ENCABEZADO =====================

        [ObservableProperty] private string usuario = string.Empty;
        [ObservableProperty] private string perfil = string.Empty;
        [ObservableProperty] private bool cargando;

        [ObservableProperty] private string id = string.Empty;
        [ObservableProperty] private DateTime fecha;
        [ObservableProperty] private string estatus = string.Empty;
        [ObservableProperty] private string observaciones = string.Empty;

        [ObservableProperty] private string nombrePredio = string.Empty;
        [ObservableProperty] private string nombreVehiculo = string.Empty;
        [ObservableProperty] private string nombreTractorCuadrilla = string.Empty;
        [ObservableProperty] private string jefeCuadrilla = string.Empty;
        [ObservableProperty] private string tipoCuadrilla = string.Empty;

        [ObservableProperty] private bool esExterno;
        [ObservableProperty] private string nombreProveedor = string.Empty;
        [ObservableProperty] private string nombreOperador = string.Empty;
        [ObservableProperty] private string codigoUnidad = string.Empty;

        [ObservableProperty] private string horasProductivas = string.Empty;
        [ObservableProperty] private string horasMuertas = string.Empty;
        [ObservableProperty] private string horasPD = string.Empty;
        [ObservableProperty] private string horometroInicial = string.Empty;
        [ObservableProperty] private string horometroFinal = string.Empty;
        [ObservableProperty] private bool camposHorasVisibles;
        [ObservableProperty] private string horasExtras = string.Empty;

        [ObservableProperty] private string imagenPath = string.Empty;
        public bool HayImagen => !string.IsNullOrEmpty(ImagenPath);

        // ===================== ESTATUS / AUTORIZACIÓN (solo lectura) =====================

        [ObservableProperty] private string usuarioCaptura = string.Empty;
        [ObservableProperty] private string observacionRechazo = string.Empty;
        [ObservableProperty] private bool hayObservacionRechazo;
        [ObservableProperty] private bool autorizadoControlInterno;
        [ObservableProperty] private bool autorizadoSuperior;

        // ===================== ACTIVIDAD REALIZADA =====================

        [ObservableProperty] private string nombreActividad = string.Empty;
        [ObservableProperty] private string nombreSubactividad = string.Empty;
        [ObservableProperty] private string cantidadActividad = string.Empty;
        [ObservableProperty] private string unidadActividad = string.Empty;
        [ObservableProperty] private string noPlantas = string.Empty;
        [ObservableProperty] private string noPersonas = string.Empty;

        // ===================== LISTAS DE DETALLE =====================

        [ObservableProperty] private ObservableCollection<MateriaPrimaUtilizadoDetalleModel> materiasPrimas = new();
        [ObservableProperty] private bool hayMateriasPrimas;

        [ObservableProperty] private ObservableCollection<InsumoUtilizadoDetalleModel> insumos = new();
        [ObservableProperty] private bool hayInsumos;

        [ObservableProperty] private ObservableCollection<ImplementoUtilizadoDetalleModel> implementos = new();
        [ObservableProperty] private bool hayImplementos;

        // ===================== POPUP DE RECHAZO (overlay en la misma página) =====================

        [ObservableProperty] private bool mostrarPopupRechazo;
        [ObservableProperty] private string motivoRechazo = string.Empty;

        [RelayCommand]
        private void AbrirRechazo()
        {
            MotivoRechazo = string.Empty;
            MostrarPopupRechazo = true;
        }

        [RelayCommand]
        private void CancelarRechazo()
        {
            MostrarPopupRechazo = false;
            MotivoRechazo = string.Empty;
        }

        [RelayCommand]
        private async Task ConfirmarRechazoAsync()
        {
            if (string.IsNullOrWhiteSpace(MotivoRechazo))
            {
                await Shell.Current.DisplayAlert("Advertencia", "Es necesario capturar el motivo del rechazo.", "De acuerdo");
                return;
            }

            Cargando = true;
            try
            {
                var modelo = new RechazarRegistroActividadModel
                {
                    IntAGRRegistroActividadKey = ActividadId,
                    VchObservacionRechazo = MotivoRechazo,
                    VchLogin = _sesionApp.Login
                };

                await _actividadService.RechazarRegistroActividadAsync(modelo);

                MostrarPopupRechazo = false;
                await Shell.Current.DisplayAlert("Éxito", "El registro fue rechazado.", "De acuerdo");
                await Shell.Current.GoToAsync("..");
            }
            catch (HttpRequestException ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "De acuerdo");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "Ocurrió un problema al rechazar el registro. Intenta de nuevo.", "De acuerdo");
            }
            finally
            {
                Cargando = false;
            }
        }

        // ===================== AUTORIZAR =====================

        [RelayCommand]
        private async Task AutorizarAsync()
        {
            bool confirmar = await Shell.Current.DisplayAlert(
                "Autorizar", "¿Deseas autorizar este registro?", "Autorizar", "Cancelar");

            if (!confirmar) return;

            Cargando = true;
            try
            {
                var modelo = new AutorizarRegistroActividadModel
                {
                    IntAGRRegistroActividadKey = ActividadId,
                    VchAutoriza = "Superior",
                    VchLogin = _sesionApp.Login
                };

                await _actividadService.AutorizarRegistroActividadAsync(modelo);

                await Shell.Current.DisplayAlert("Éxito", "El registro fue autorizado.", "De acuerdo");
                await Shell.Current.GoToAsync("..");
            }
            catch (HttpRequestException ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "De acuerdo");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "Ocurrió un problema al autorizar el registro. Intenta de nuevo.", "De acuerdo");
            }
            finally
            {
                Cargando = false;
            }
        }

        [RelayCommand]
        private async Task CerrarAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        // ===================== INICIALIZACIÓN =====================

        private bool _yaInicializado;

        public async Task InicializarAsync()
        {
            if (_yaInicializado) return;
            _yaInicializado = true;

            Cargando = true;
            try
            {
                Usuario = _sesionApp.NombreUsuario;
                Perfil = _sesionApp.Perfil;

                if (_registroActividadOriginal is null)
                {
                    await Shell.Current.DisplayAlert("Error", "No fue posible cargar la información del registro. Regresa e intenta de nuevo.", "De acuerdo");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                var reg = _registroActividadOriginal;

                // ── Encabezado (datos que ya trae el registro, sin llamadas extra) ──
                Id = reg.VchID;
                Fecha = reg.DtmFecha;
                Estatus = reg.VchEstatus;
                Observaciones = reg.VchObservacionActividad;
                JefeCuadrilla = reg.VchJefeCuadrilla;
                TipoCuadrilla = reg.VchTipoCuadrilla;
                EsExterno = reg.VchTipoCuadrilla != null && reg.VchTipoCuadrilla.StartsWith("Ext", StringComparison.OrdinalIgnoreCase);
                CodigoUnidad = reg.VchCodigoUnidad ?? string.Empty;
                HorometroInicial = reg.VchHorometroInicial ?? string.Empty;
                HorometroFinal = reg.VchHorometroFinal ?? string.Empty;
                HorasProductivas = reg.VchHrsProductivasInicial ?? string.Empty;
                HorasMuertas = reg.VchHrsProductivasFinal ?? string.Empty;
                CamposHorasVisibles = !string.IsNullOrEmpty(reg.VchHorometroInicial) && reg.VchHorometroInicial != "00:00:00";
                HorasExtras = reg.DecHorasExtras.ToString("0.##");

                UsuarioCaptura = reg.VchUsuarioCaptura ?? string.Empty;
                ObservacionRechazo = reg.VchObservacionRechazo ?? string.Empty;
                HayObservacionRechazo = !string.IsNullOrWhiteSpace(reg.VchObservacionRechazo);
                AutorizadoControlInterno = reg.BitAutorizaControlInterno;
                AutorizadoSuperior = reg.BitAutorizaSuperior;

                if (!string.IsNullOrEmpty(reg.VchNombreImagen))
                    ImagenPath = reg.VchNombreImagen;

                // ── Catálogos para resolver nombres (Predio/Vehículo/Cuadrilla/Proveedor/Operador) ──
                var tareaPredios = _catalogoCacheService.ObtenerAsync("PrediosCatalogo", _actividadService.ObtenerPrediosCatalogoAsync);
                var tareaVehiculos = _catalogoCacheService.ObtenerAsync("Vehiculos", _actividadService.ObtenerVehiculosAsync);
                var tareaTractores = _catalogoCacheService.ObtenerAsync("TractoresCuadrillas", _actividadService.ObtenerTractoresCuadrillasAsync);

                await Task.WhenAll(tareaPredios, tareaVehiculos, tareaTractores);

                var predio = tareaPredios.Result.FirstOrDefault(p => p.IntGENPredioKey == reg.IntGENPredioLink);
                NombrePredio = predio != null ? $"{predio.VchAuxiliarCodigo} - {predio.VchNombre}" : string.Empty;

                var vehiculo = tareaVehiculos.Result.FirstOrDefault(v => v.IntGENUnidadParaActividadKey == reg.IntGENUnidadParaActividadLink);
                NombreVehiculo = vehiculo?.VchNombreCompleto ?? string.Empty;

                var tractorCuadrilla = tareaTractores.Result.FirstOrDefault(t => t.IntAGRTractoresCuadrillasKey == reg.IntAGRTractoresCuadrillasLink);
                NombreTractorCuadrilla = tractorCuadrilla?.VchDescripcion ?? string.Empty;

                if (EsExterno)
                {
                    var tareaProveedores = _catalogoCacheService.ObtenerAsync("Proveedores", _actividadService.ObtenerProveedoresAsync);
                    var proveedores = await tareaProveedores;
                    var proveedor = proveedores.FirstOrDefault(p => p.IntGENProveedorKey == reg.IntGENProveedorLink);
                    NombreProveedor = proveedor?.VchRazonSocial ?? string.Empty;

                    if (proveedor != null && reg.IntGENOperadorMaquinariaLink.HasValue)
                    {
                        var operadores = await _actividadService.ObtenerOperadoresMaquinariaAsync(proveedor.IntGENProveedorKey);
                        var operador = operadores.FirstOrDefault(o => o.IntGENOperadorMaquinariaKey == reg.IntGENOperadorMaquinariaLink);
                        NombreOperador = operador?.VchNombre ?? string.Empty;
                    }
                }

                // ── Detalle: materia prima, insumos, implementos (en paralelo) ──
                var tareaMateriasPrimas = _actividadService.ObtenerMateriasPrimasUtilizadasAsync(reg.IntAGRRegistroActividadKey);
                var tareaInsumos = _actividadService.ObtenerInsumosUtilizadosAsync(reg.IntAGRRegistroActividadKey);
                var tareaImplementos = _actividadService.ObtenerImplementosUtilizadosAsync(reg.IntAGRRegistroActividadKey);

                // ── Actividad Realizada: la mayoría de los registros NO son de Preparación de
                // Terreno, así que se intenta primero como actividad normal (false) y solo si
                // no trae datos se reintenta como PT (true). Mismo fallback que en el Xamarin
                // legacy (AutorizacionSuperiorOpciones.OnAppearing), solo invertido en orden
                // porque ahí el caso frecuente era el contrario. ──
                var actividadRealizada = await _actividadService.ObtenerActividadRealizadaAsync(reg.IntAGRRegistroActividadKey, preparacionTerreno: false);
                if (actividadRealizada is null)
                    actividadRealizada = await _actividadService.ObtenerActividadRealizadaAsync(reg.IntAGRRegistroActividadKey, preparacionTerreno: true);

                await Task.WhenAll(tareaMateriasPrimas, tareaInsumos, tareaImplementos);

                if (actividadRealizada != null)
                {
                    NombreActividad = actividadRealizada.VchActividad ?? string.Empty;
                    NombreSubactividad = actividadRealizada.VchSubActividad ?? string.Empty;
                    CantidadActividad = actividadRealizada.DecValor.ToString();
                    UnidadActividad = actividadRealizada.VchUnidad ?? string.Empty;
                    NoPlantas = actividadRealizada.DecNoPlantas.ToString();
                    NoPersonas = actividadRealizada.DecNoPersonas.ToString();
                    HorasPD = actividadRealizada.DecHorasPD.ToString("0.####");

                    // Si el detalle trae Horas Productivas/Muertas propias (más específicas
                    // que el encabezado del registro), se usan estas.
                    if (!string.IsNullOrEmpty(actividadRealizada.VchHrsProductivas))
                        HorasProductivas = actividadRealizada.VchHrsProductivas;
                    if (!string.IsNullOrEmpty(actividadRealizada.VchHrsMuertas))
                        HorasMuertas = actividadRealizada.VchHrsMuertas;
                }

                MateriasPrimas = new ObservableCollection<MateriaPrimaUtilizadoDetalleModel>(tareaMateriasPrimas.Result);
                HayMateriasPrimas = MateriasPrimas.Count > 0;

                Insumos = new ObservableCollection<InsumoUtilizadoDetalleModel>(tareaInsumos.Result);
                HayInsumos = Insumos.Count > 0;

                Implementos = new ObservableCollection<ImplementoUtilizadoDetalleModel>(tareaImplementos.Result);
                HayImplementos = Implementos.Count > 0;
            }
            catch (HttpRequestException)
            {
                await Shell.Current.DisplayAlert("Sin conexión", "No fue posible conectarse al servidor. Verifica tu conexión a internet.", "De acuerdo");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error (debug)", ex.ToString(), "De acuerdo");
            }
            finally
            {
                Cargando = false;
            }
        }
    }
}