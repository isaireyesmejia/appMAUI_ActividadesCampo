using agaverosActividades.Models;
using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using agaverosActividades.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace agaverosActividades.ViewModels
{
    /// <summary>
    /// ViewModel de RegistroActividadPrepTerrenoFormPage (Alta y Edición).
    ///
    /// Diferencias clave frente a RegistroActividadFormViewModel:
    /// - La Actividad es FIJA (IntAGRActividadKey == 64, "Preparación de Terreno"), no se selecciona.
    /// - No hay materia prima / insumo / vale de salida.
    /// - Solo se permite UN implemento por captura, capturado directo (Equipo + Cantidad),
    ///   sin botón "Agregar" ni tarjeta de confirmación — se valida y se envía junto con
    ///   el resto del formulario al tocar "Guardar". Misma regla de negocio del portal web
    ///   (1 implemento por registro), solo que la UX ya no exige un paso de "commit" aparte.
    ///   Como es un solo objeto (no lista), en modo Edición se rastrea su key original
    ///   (_implementoUtilizadoKeyOriginal / _implementoLinkOriginal) para poder mandar
    ///   Agregar / Actualizar / Eliminar según lo que el usuario deje, cambie o quite.
    /// - Se agregan Horas Productivas / Horas Muertas / Horas PD, Operador de Maquinaria y Código de Unidad.
    /// - Se agrega Estatus (En Proceso / Terminado), obligatorio para guardar.
    ///
    /// MODO EDICIÓN (agregado): replica el mismo patrón de RegistroActividadFormViewModel
    /// (IQueryAttributable + _registroActividadOriginal + bifurcación Alta/Edición en Guardar()).
    /// Se navega en modo Editar con idActividad + parámetro "registroActividad" (ver
    /// RegistroActividadesPrepTerrenoViewModel.EditarAsync).
    ///
    /// PENDIENTE DE CONFIRMAR:
    /// - Que ObtenerActividadesSinCosechaAsync()/ObtenerActividadesCatalogoAsync() realmente
    ///   incluya la actividad clave 64 (ver InicializarAsync).
    /// </summary>
    [QueryProperty(nameof(PredioId), "idPredio")]
    [QueryProperty(nameof(ZonaId), "idZona")]
    [QueryProperty(nameof(MunicipioId), "idMunicipio")]
    [QueryProperty(nameof(ActividadId), "idActividad")]
    public partial class RegistroActividadPrepTerrenoFormViewModel : ObservableObject, IQueryAttributable
    {
        /// <summary>Clave fija de la actividad "Preparación de Terreno" (igual que en Xamarin).</summary>
        private const int CLAVE_ACTIVIDAD_PREP_TERRENO = 64;

        private readonly IActividadService _actividadService;
        private readonly ISesionApp _sesionApp;
        private readonly IMediaService _mediaService;
        private readonly ILocalDataService _localDataService;
        private readonly ICatalogoCacheService _catalogoCacheService;

        private RegistroActividadModel? _registroActividadOriginal;

        /// <summary>PASO 8 (igual que en el general): key real de AGRActividadRealizada en el
        /// servidor, necesaria para mandar IntMovimiento = Actualizar con la key correcta.</summary>
        private int _actividadRealizadaKeyOriginal;

        /// <summary>Key original del implemento único capturado (0 si el registro no traía implemento).</summary>
        private int _implementoUtilizadoKeyOriginal;

        /// <summary>Link de implemento original, para poder mandarlo en el caso Eliminar.</summary>
        private int _implementoLinkOriginal;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("registroActividad", out var value) && value is RegistroActividadModel modelo)
                _registroActividadOriginal = modelo;
        }

        public RegistroActividadPrepTerrenoFormViewModel(
            IActividadService actividadService,
            ISesionApp sesionApp,
            IMediaService mediaService,
            ILocalDataService localDataService,
            ICatalogoCacheService catalogoCacheService)
        {
            _actividadService = actividadService;
            _sesionApp = sesionApp;
            _mediaService = mediaService;
            _localDataService = localDataService;
            _catalogoCacheService = catalogoCacheService;
        }

        // ===================== PARÁMETROS DE NAVEGACIÓN =====================

        [ObservableProperty] private int predioId;
        [ObservableProperty] private int zonaId;
        [ObservableProperty] private int municipioId;

        /// <summary>Si viene con valor (> 0), la página abre en modo Editar y carga el registro existente.</summary>
        [ObservableProperty] private int actividadId;

        // ===================== ENCABEZADO =====================

        [ObservableProperty] private string tituloPagina = "Alta de Preparación de Terreno";
        [ObservableProperty] private string usuario;
        [ObservableProperty] private string perfil;
        [ObservableProperty] private string hoy;
        [ObservableProperty] private bool cargando;
        [ObservableProperty] private bool frameDetallesVisible;

        // ===================== DATOS GENERALES =====================

        [ObservableProperty] private string id;
        [ObservableProperty] private DateTime fecha = DateTime.Now;

        /// <summary>True cuando el formulario abrió en modo Edición.</summary>
        [ObservableProperty] private bool esEdicion;

        [ObservableProperty] private ObservableCollection<PredioModel> predios = new();
        [ObservableProperty] private PredioModel predioSeleccionado;

        partial void OnPredioSeleccionadoChanged(PredioModel value)
        {
            Id = value != null ? $"{value.VchCodigo}-0000" : string.Empty;
        }

        [ObservableProperty] private ObservableCollection<VehiculoActividadModel> vehiculos = new();
        [ObservableProperty] private VehiculoActividadModel vehiculoSeleccionado;

        [ObservableProperty] private ObservableCollection<TractorCuadrillaModel> tractoresCuadrillas = new();
        [ObservableProperty] private TractorCuadrillaModel tractorCuadrillaSeleccionado;

        /// <summary>Controla si se muestran Horas Prod./Muertas y Producción Inicial/Final (solo Tractor/Maquinaria).</summary>
        [ObservableProperty] private bool camposHorasHabilitados;
        /// <summary>Solo "Tractor" exacto habilita la sección de Equipo/Implemento (regla exacta del portal, ver ValidaComboBoxImplementos).</summary>
        [ObservableProperty] private bool equipoHabilitado;

        partial void OnTractorCuadrillaSeleccionadoChanged(TractorCuadrillaModel value)
        {
            bool esTractorOMaquinaria = value != null &&
                (value.VchDescripcion == "Tractor" || value.VchDescripcion == "Maquinaria");

            CamposHorasHabilitados = esTractorOMaquinaria;
            EquipoHabilitado = value != null && value.VchDescripcion == "Tractor";

            if (!esTractorOMaquinaria)
            {
                HorasProductivas = string.Empty;
                HorasMuertas = string.Empty;
                HorasPD = "0.00";
                ProduccionInicial = TimeSpan.Zero;
                ProduccionFinal = TimeSpan.Zero;
            }

            if (!EquipoHabilitado)
            {
                EquipoSeleccionado = null;
                CantidadEquipo = "1";
            }
        }

        [ObservableProperty] private ObservableCollection<JefeCuadrillaModel> jefesCuadrilla = new();
        [ObservableProperty] private JefeCuadrillaModel jefeCuadrillaSeleccionado;

        // ===================== TIPO DE CUADRILLA (Interno/Externo) =====================

        [ObservableProperty] private bool esInterno;
        [ObservableProperty] private bool esExterno;
        [ObservableProperty] private bool proveedorHabilitado;

        [ObservableProperty] private ObservableCollection<ProveedorModel> proveedores = new();
        [ObservableProperty] private ProveedorModel proveedorSeleccionado;

        [ObservableProperty] private ObservableCollection<OperadorMaquinariaModel> operadoresMaquinaria = new();
        [ObservableProperty] private OperadorMaquinariaModel operadorSeleccionado;

        [ObservableProperty] private string codigoUnidad;

        [RelayCommand]
        private void MarcarInterno()
        {
            EsInterno = true;
            EsExterno = false;
            ProveedorHabilitado = false;
            ProveedorSeleccionado = null;
            OperadorSeleccionado = null;
            OperadoresMaquinaria = new ObservableCollection<OperadorMaquinariaModel>();
            CodigoUnidad = string.Empty;
        }

        [RelayCommand]
        private void MarcarExterno()
        {
            EsInterno = false;
            EsExterno = true;
            ProveedorHabilitado = true;
        }

        partial void OnProveedorSeleccionadoChanged(ProveedorModel value)
        {
            if (value != null && !EsExterno)
                MarcarExterno();

            // Nota: en modo Edición, CargarRegistroExistenteAsync fija OperadorSeleccionado
            // DESPUÉS de fijar ProveedorSeleccionado y de que CargarOperadoresAsync termine,
            // para que este reseteo no lo borre (ver CargarRegistroExistenteAsync).
            OperadorSeleccionado = null;
            OperadoresMaquinaria = new ObservableCollection<OperadorMaquinariaModel>();

            if (value != null)
                _ = CargarOperadoresAsync(value.IntGENProveedorKey);
        }

        private async Task CargarOperadoresAsync(int idProveedor)
        {
            try
            {
                // FIX (offline): antes llamaba directo a _actividadService.ObtenerOperadoresMaquinariaAsync,
                // sin pasar por el cache, así que sin conexión siempre tronaba con "No se pudieron cargar
                // los operadores de maquinaria". Ahora usa ICatalogoCacheService igual que el resto de los
                // catálogos, con key por proveedor (solo sirve offline si ya se consultó este proveedor
                // al menos una vez con conexión).
                var lista = await _catalogoCacheService.ObtenerAsync(
                    $"OperadoresMaquinaria_{idProveedor}",
                    ct => _actividadService.ObtenerOperadoresMaquinariaAsync(idProveedor, ct));
                OperadoresMaquinaria = new ObservableCollection<OperadorMaquinariaModel>(lista);
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert(
                    "Error",
                    "No se pudieron cargar los operadores de maquinaria. Si estás sin conexión, este proveedor aún no se había consultado en línea desde este dispositivo.",
                    "De acuerdo");
            }
        }

        // ===================== ACTIVIDAD (fija) / SUBACTIVIDAD =====================

        /// <summary>Actividad fija resuelta en InicializarAsync (clave 64). No se muestra combo para elegirla.</summary>
        private ActividadAgricolaModel _actividadFija;

        [ObservableProperty] private ObservableCollection<SubactividadModel> subactividades = new();
        [ObservableProperty] private SubactividadModel subactividadSeleccionada;

        partial void OnSubactividadSeleccionadaChanged(SubactividadModel value)
        {
            UnidadActividad = value?.VchUnidadDeMedida ?? string.Empty;
        }

        [ObservableProperty] private string cantidadActividad;
        [ObservableProperty] private string unidadActividad;
        [ObservableProperty] private string noPlantas;
        [ObservableProperty] private string noPersonas;

        // ===================== EQUIPO / IMPLEMENTO (captura directa, uno solo) =====================
        // Se envía junto con el resto del registro en Guardar(); no hay lista ni tarjeta intermedia.

        [ObservableProperty] private ObservableCollection<EquipoModel> equipos = new();
        [ObservableProperty] private EquipoModel equipoSeleccionado;
        [ObservableProperty] private string cantidadEquipo = "1";

        // TODO CONFIRMAR: el XAML bindea este Picker de "Unidad" pero el ViewModel original de Alta
        // nunca lo poblaba ni lo usaba en el payload (GuardarAltaAsync no lo envía). Se agregan las
        // propiedades para que compile; dime si debe cargarse de un catálogo (¿ObtenerUnidadesActividadAsync,
        // igual que en RegistroActividadFormViewModel?) o derivarse de EquipoSeleccionado, y si debe
        // viajar en ImplementoUtilizadoModel.
        [ObservableProperty] private ObservableCollection<UnidadEquipoModel> unidadesEquipo = new();
        [ObservableProperty] private UnidadEquipoModel unidadEquipoSeleccionada;

        /// <summary>Limpia la selección de Equipo (ícono "✕" junto al Picker), ya que algunos Pickers
        /// de MAUI no permiten deseleccionar tocando de nuevo el mismo valor. En modo Edición, si el
        /// registro ya traía un implemento guardado, esto se traduce en Eliminar al guardar
        /// (ver GuardarEdicionAsync), no se pierde el rastro porque _implementoUtilizadoKeyOriginal
        /// no se toca aquí.</summary>
        [RelayCommand]
        private void LimpiarEquipo()
        {
            EquipoSeleccionado = null;
            CantidadEquipo = "1";
        }
        /// <summary>Abre el selector con buscador para Equipo, igual que SeleccionarPreparacion en
        /// RegistroActividadFormViewModel. No hace nada si EquipoHabilitado es false (el campo solo
        /// aplica cuando la cuadrilla es "Tractor"), mismo comportamiento que tenía el Picker deshabilitado.</summary>
        [RelayCommand]
        private async Task SeleccionarEquipo()
        {
            if (!EquipoHabilitado) return;

            var seleccion = await agaverosActividades.Views.SearchablePickerPage.MostrarAsync(
                titulo: "Equipo",
                items: Equipos,
                textoMostrar: e => e.VchNombreConCodigo);

            if (seleccion != null)
                EquipoSeleccionado = seleccion;
        }
        // ===================== ESTATUS (En Proceso / Terminado) =====================

        [ObservableProperty] private bool enProceso;
        [ObservableProperty] private bool terminado;

        [RelayCommand]
        private void MarcarEnProceso() { EnProceso = true; Terminado = false; }

        [RelayCommand]
        private void MarcarTerminado() { EnProceso = false; Terminado = true; }

        private string Estatus => EnProceso ? "En Proceso" : Terminado ? "Terminado" : string.Empty;

        // ===================== HORAS / HORÓMETRO =====================

        [ObservableProperty] private string horasProductivas = string.Empty;
        [ObservableProperty] private string horasMuertas = string.Empty;
        [ObservableProperty] private string horasPD = "0.00";

        /// <summary>
        /// Formatea a "H:MM" según se va tecleando (puerto de txtHorasProdFrame_TextChanged de Xamarin).
        /// Se invoca desde el code-behind en el evento TextChanged del Entry, ya que el formateo
        /// depende de la posición del cursor y no es un simple binding TwoWay.
        /// </summary>
        public string FormatearHoras(string textoNuevo)
        {
            var soloDigitos = new string((textoNuevo ?? string.Empty).Where(char.IsDigit).ToArray());

            if (soloDigitos.Length > 5)
                soloDigitos = soloDigitos.Substring(soloDigitos.Length - 5);

            if (soloDigitos.Length > 2)
            {
                var horas = soloDigitos.Substring(0, soloDigitos.Length - 2);
                var minutos = soloDigitos.Substring(soloDigitos.Length - 2);

                if (int.TryParse(minutos, out var min) && min > 59)
                    minutos = "59";

                return $"{horas}:{minutos}";
            }

            return soloDigitos;
        }

        partial void OnHorasProductivasChanged(string value)
        {
            HorasPD = CalcularHorasPD(value);
        }

        /// <summary>Puerto directo de ObtenerHorasPD() del Xamarin legacy.</summary>
        private static string CalcularHorasPD(string horasProductivasTexto)
        {
            var texto = (horasProductivasTexto ?? string.Empty).Trim();

            if (texto == "0:00" || string.IsNullOrEmpty(texto))
                return "0.00";

            var limpio = texto.Replace(":00:00", ":00").Replace(":", ".");

            if (string.IsNullOrEmpty(limpio) || !decimal.TryParse(limpio, out var horas))
                return "0.00";

            var parteEntera = Math.Truncate(horas);
            if (parteEntera > 9999)
                return "0.00";

            var parteDecimal = ((horas - parteEntera) * 100) / 60;
            return Math.Round(parteEntera + parteDecimal, 4, MidpointRounding.ToEven).ToString("0.####");
        }

        [ObservableProperty] private string inicial = "0";
        [ObservableProperty] private string final = "0";
        [ObservableProperty] private TimeSpan produccionInicial;
        [ObservableProperty] private TimeSpan produccionFinal;

        // ===================== IMAGEN / OBSERVACIONES =====================
        // Idéntico patrón a RegistroActividadFormViewModel (reutiliza IMediaService / SubirImagenAsync).

        [ObservableProperty] private string observaciones;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HayImagen))]
        private string imagenPath;

        private string imagenNombre;

        /// <summary>PASADA 5 (igual que en el general): referencia de la imagen tal cual venía del
        /// servidor al entrar en modo Edición. Se usa en GuardarEdicionAsync para saber si el usuario
        /// dejó la imagen intacta (se reenvía esta misma referencia sin volver a subir nada) o la
        /// reemplazó por una nueva (ahí sí se sube). Queda null en modo Alta.</summary>
        private string _rutaArchivoImagenOriginal;
        private string _nombreImagenOriginal;

        public bool HayImagen => !string.IsNullOrEmpty(ImagenPath);

        public event Func<string, Task> SolicitarMostrarImagen;

        /// <summary>PASADA 5: true si path es una URL absoluta http/https (imagen ya guardada en servidor).</summary>
        private static bool EsImagenRemota(string path) =>
            !string.IsNullOrEmpty(path) &&
            Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https");

        private void BorrarImagenLocalPendiente(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                return;

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BorrarImagenLocalPendiente error: {ex}");
            }
        }

        [RelayCommand]
        private async Task TomarFoto()
        {
            var pathAnterior = ImagenPath;
            var path = await _mediaService.TomarFotoAsync();
            if (path != null)
            {
                BorrarImagenLocalPendiente(pathAnterior);
                ImagenPath = path;
                imagenNombre = Path.GetFileName(path);
            }
        }

        [RelayCommand]
        private async Task ElegirDeGaleria()
        {
            var pathAnterior = ImagenPath;
            var path = await _mediaService.ElegirDeGaleriaAsync();
            if (path != null)
            {
                BorrarImagenLocalPendiente(pathAnterior);
                ImagenPath = path;
                imagenNombre = Path.GetFileName(path);
            }
        }

        [RelayCommand]
        private async Task VerImagen()
        {
            if (!string.IsNullOrEmpty(ImagenPath) && SolicitarMostrarImagen != null)
                await SolicitarMostrarImagen(ImagenPath);
        }

        [RelayCommand]
        private void QuitarImagen()
        {
            BorrarImagenLocalPendiente(ImagenPath);
            ImagenPath = null;
            imagenNombre = null;
        }

        // ===================== ACCIONES PRINCIPALES =====================

        [ObservableProperty] private bool guardarHabilitado = true;

        [RelayCommand]
        private async Task Guardar()
        {
            if (VehiculoSeleccionado is null || TractorCuadrillaSeleccionado is null || JefeCuadrillaSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Completa unidad, cuadrilla/tractor y jefe de cuadrilla.", "De acuerdo");
                return;
            }

            if (PredioSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "No hay predio seleccionado.", "De acuerdo");
                return;
            }

            if (_actividadFija is null || SubactividadSeleccionada is null || string.IsNullOrWhiteSpace(CantidadActividad))
            {
                await Shell.Current.DisplayAlert("Advertencia", "Completa la Subactividad y la Cantidad de la actividad realizada.", "De acuerdo");
                return;
            }

            if (!decimal.TryParse(CantidadActividad, out var cantidadActividad) || cantidadActividad <= 0)
            {
                await Shell.Current.DisplayAlert("Advertencia", "La Cantidad de la actividad debe ser un valor mayor a cero.", "De acuerdo");
                return;
            }

            if (!decimal.TryParse(NoPersonas, out var noPersonasVal) || noPersonasVal <= 0)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Es necesario el valor No. Personas y debe ser mayor a cero.", "De acuerdo");
                return;
            }

            if (CamposHorasHabilitados)
            {
                if (string.IsNullOrWhiteSpace(HorasProductivas) || HorasProductivas == "0:00")
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Es necesario capturar las Horas Productivas.", "De acuerdo");
                    return;
                }

                if (string.IsNullOrWhiteSpace(HorasMuertas) || HorasMuertas == "0:00")
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Es necesario capturar las Horas Muertas.", "De acuerdo");
                    return;
                }
            }

            if (EquipoSeleccionado != null && (!decimal.TryParse(CantidadEquipo, out var cantidadEquipoVal) || cantidadEquipoVal <= 0))
            {
                await Shell.Current.DisplayAlert("Advertencia", "Ingresa una cantidad válida de equipo.", "De acuerdo");
                return;
            }

            if (CamposHorasHabilitados && ProduccionFinal <= ProduccionInicial)
            {
                await Shell.Current.DisplayAlert("Error", "La hora final no puede ser menor o igual que la hora inicial.", "Confirmar");
                return;
            }

            if (TractorCuadrillaSeleccionado?.VchDescripcion == "Tractor")
            {
                if (ProduccionInicial == TimeSpan.Zero)
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Es necesario capturar el Horómetro inicial.", "De acuerdo");
                    return;
                }

                if (ProduccionFinal == TimeSpan.Zero)
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Es necesario capturar el Horómetro final.", "De acuerdo");
                    return;
                }
            }

            if (EsExterno && ProveedorSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona un proveedor para cuadrilla externa.", "De acuerdo");
                return;
            }

            if (EsExterno && OperadorSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona un operador de maquinaria para cuadrilla externa.", "De acuerdo");
                return;
            }

            if (!EsInterno && !EsExterno)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Es necesario seleccionar un Tipo de Cuadrilla.", "De acuerdo");
                return;
            }

            if (string.IsNullOrEmpty(Estatus))
            {
                await Shell.Current.DisplayAlert("Advertencia", "Es necesario seleccionar un Estatus.", "De acuerdo");
                return;
            }

            Cargando = true;
            try
            {
                if (EsEdicion)
                    await GuardarEdicionAsync(cantidadActividad);
                else
                    await GuardarAltaAsync(cantidadActividad);
            }
            catch (HttpRequestException ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "De acuerdo");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "Ocurrió un problema al guardar. Intenta de nuevo.", "De acuerdo");
            }
            finally
            {
                Cargando = false;
            }
        }

        private async Task GuardarAltaAsync(decimal cantidadActividad)
        {
            var altaModelo = new AltaRegistroActividadModel
            {
                IntGENUnidadParaActividadLink = VehiculoSeleccionado.IntGENUnidadParaActividadKey,
                IntGENPredioLink = PredioSeleccionado.IntGENPredioKey,
                IntAGRTractoresCuadrillasLink = TractorCuadrillaSeleccionado.IntAGRTractoresCuadrillasKey,
                VchID = Id,
                DtmFecha = Fecha,
                VchJefeCuadrilla = JefeCuadrillaSeleccionado.VchNombre,
                VchTipoCuadrilla = EsInterno ? "Interna" : "Externa",
                VchObservaciones = Observaciones ?? string.Empty,
                VchLogin = _sesionApp.Login,
                DecHorasExtras = null,
                VchEstatus = Estatus,
                VchHrsProductivasInicial = HorasProductivas,
                VchHrsProductivasFinal = HorasMuertas,
                VchHorometroInicial = ProduccionInicial.ToString(@"hh\:mm"),
                VchHorometroFinal = ProduccionFinal.ToString(@"hh\:mm"),
                IntGENProveedorLink = ProveedorSeleccionado?.IntGENProveedorKey ?? -1,
                IntGENOperadorMaquinariaLink = OperadorSeleccionado?.IntGENOperadorMaquinariaKey,
                VchCodigoUnidad = CodigoUnidad ?? string.Empty
            };

            var actividadModelo = new ActividadRealizadaModel
            {
                IntMovimiento = 1,
                IntAGRActividadRealizadaKey = 0,
                IntAGRRegistroActividadLink = 0,
                IntAGRActividadLink = _actividadFija.IntAGRActividadKey,
                IntAGRSubActividadLink = SubactividadSeleccionada.IntAGRSubActividadKey,
                DecValor = cantidadActividad,
                DecNoPlantas = decimal.TryParse(NoPlantas, out var plantas) ? plantas : 0,
                DecNoPersonas = decimal.TryParse(NoPersonas, out var personas) ? personas : 0,
                VchObservaciones = Observaciones ?? string.Empty,
                VchHrsProductivas = HorasProductivas,
                VchHrsMuertas = HorasMuertas,
                DecHorasPD = decimal.TryParse(HorasPD, out var horasPD) ? horasPD : 0,
                VchNombreImagen = string.Empty,
                VchRutaArchivo = string.Empty,
                VchUsuario = _sesionApp.Login
            };

            // Implemento: captura directa desde Equipo/Cantidad (sin lista, sin tarjeta intermedia).
            // Misma regla de negocio del portal: como máximo 1 implemento por registro.
            var implementosDto = EquipoSeleccionado != null
                ? new List<ImplementoUtilizadoModel>
                {
                    new ImplementoUtilizadoModel
                    {
                        IntMovimiento = 1,
                        IntAGRImplementoUtilizadoKey = 0,
                        IntAGRRegistroActividadLink = 0,
                        IntGENImplementoLink = EquipoSeleccionado.IntGENImplementoKey,
                        DecCantidad = decimal.TryParse(CantidadEquipo, out var cantEquipo) ? cantEquipo : 1,
                        VchUsuario = _sesionApp.Login
                    }
                }
                : new List<ImplementoUtilizadoModel>();

            var payload = new GuardarRegistroActividadPayload
            {
                Descripcion = $"Preparación de Terreno - {PredioSeleccionado.VchNombre} - {Fecha:dd/MM/yyyy}",
                Alta = altaModelo,
                ActividadRealizada = actividadModelo,
                Insumos = new List<InsumoUtilizadoModel>(),
                Implementos = implementosDto,
                MateriaPrima = new List<MateriaPrimaUtilizadoModel>(),
                ImagenPathLocal = !string.IsNullOrEmpty(ImagenPath) ? ImagenPath : null,
                ImagenNombre = imagenNombre
            };

            var resultado = await _localDataService.GuardarAsync(payload);

            if (resultado.GuardadoEnLinea)
            {
                await Shell.Current.DisplayAlert("Éxito", $"Registro guardado con folio {resultado.FolioServidor}.", "De acuerdo");
            }
            else if (resultado.Encolado)
            {
                await Shell.Current.DisplayAlert(
                    "Guardado sin conexión",
                    "No hay conexión a internet. El registro se guardó en este dispositivo y se enviará al servidor cuando sincronices.",
                    "De acuerdo");
            }

            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Modo Edición. Misma estructura que RegistroActividadFormViewModel.GuardarEdicionAsync,
        /// adaptada a un solo implemento (no lista): se resuelve Agregar/Actualizar/Eliminar
        /// comparando contra _implementoUtilizadoKeyOriginal / _implementoLinkOriginal capturados
        /// en CargarRegistroExistenteAsync.
        /// </summary>
        private async Task GuardarEdicionAsync(decimal cantidadActividad)
        {
            // ── Paso 1: encabezado a actualizar ──────────────────────────
            var actualizarModelo = new ActualizarRegistroActividadModel
            {
                IntAGRRegistroActividadKey = ActividadId,
                IntGENUnidadParaActividadLink = VehiculoSeleccionado.IntGENUnidadParaActividadKey,
                IntGENPredioLink = PredioSeleccionado.IntGENPredioKey,
                IntAGRTractoresCuadrillasLink = TractorCuadrillaSeleccionado.IntAGRTractoresCuadrillasKey,
                VchID = Id,
                DtmFecha = Fecha,
                VchJefeCuadrilla = JefeCuadrillaSeleccionado.VchNombre,
                VchTipoCuadrilla = EsInterno ? "Interna" : "Externa",
                VchObservaciones = Observaciones ?? string.Empty,
                VchLogin = _sesionApp.Login,
                VchEstatus = Estatus,
                VchHrsProductivasInicial = HorasProductivas,
                VchHrsProductivasFinal = HorasMuertas,
                VchHorometroInicial = ProduccionInicial.ToString(@"hh\:mm"),
                VchHorometroFinal = ProduccionFinal.ToString(@"hh\:mm"),
                IntGENProveedorLink = ProveedorSeleccionado?.IntGENProveedorKey,
                IntGENOperadorMaquinariaLink = OperadorSeleccionado?.IntGENOperadorMaquinariaKey,
                VchCodigoUnidad = CodigoUnidad ?? string.Empty
            };

            // ── Paso 2: imagen — mismos 3 casos que en el form general ──
            string imagenPathLocal = null;
            string imagenUrlRemotaSinCambios = null;

            if (string.IsNullOrEmpty(ImagenPath))
            {
                // Caso a) el usuario quitó la imagen: ambos quedan null, se manda vacío.
            }
            else if (EsImagenRemota(ImagenPath) && ImagenPath == _rutaArchivoImagenOriginal)
            {
                // Caso b) dejó la misma imagen que ya estaba en servidor: se reenvía tal cual.
                imagenUrlRemotaSinCambios = _rutaArchivoImagenOriginal;
            }
            else
            {
                // Caso c) tomó foto nueva o eligió una de galería: se sube al sincronizar.
                imagenPathLocal = ImagenPath;
            }

            // ── Paso 3: actividad realizada (Actualizar, con la key existente) ──
            var actividadModelo = new ActividadRealizadaModel
            {
                IntMovimiento = 2,
                IntAGRActividadRealizadaKey = _actividadRealizadaKeyOriginal,
                IntAGRRegistroActividadLink = ActividadId,
                IntAGRActividadLink = _actividadFija.IntAGRActividadKey,
                IntAGRSubActividadLink = SubactividadSeleccionada.IntAGRSubActividadKey,
                DecValor = cantidadActividad,
                DecNoPlantas = decimal.TryParse(NoPlantas, out var plantas) ? plantas : 0,
                DecNoPersonas = decimal.TryParse(NoPersonas, out var personas) ? personas : 0,
                VchObservaciones = Observaciones ?? string.Empty,
                VchHrsProductivas = HorasProductivas,
                VchHrsMuertas = HorasMuertas,
                DecHorasPD = decimal.TryParse(HorasPD, out var horasPD) ? horasPD : 0,
                VchNombreImagen = string.Empty, // lo resuelve LocalDataService
                VchRutaArchivo = string.Empty,  // lo resuelve LocalDataService
                VchUsuario = _sesionApp.Login
            };

            // ── Paso 4: Implemento único — Agregar / Actualizar / Eliminar ──
            var implementosDto = new List<ImplementoUtilizadoModel>();

            if (EquipoSeleccionado != null)
            {
                // Ya existía en servidor -> Actualizar (misma key); si es nuevo de esta sesión -> Agregar.
                implementosDto.Add(new ImplementoUtilizadoModel
                {
                    IntMovimiento = _implementoUtilizadoKeyOriginal > 0 ? (int)MovimientoItem.Actualizar : (int)MovimientoItem.Agregar,
                    IntAGRImplementoUtilizadoKey = _implementoUtilizadoKeyOriginal,
                    IntAGRRegistroActividadLink = ActividadId,
                    IntGENImplementoLink = EquipoSeleccionado.IntGENImplementoKey,
                    DecCantidad = decimal.TryParse(CantidadEquipo, out var cantEquipo) ? cantEquipo : 1,
                    VchUsuario = _sesionApp.Login
                });
            }
            else if (_implementoUtilizadoKeyOriginal > 0)
            {
                // Traía implemento guardado y el usuario lo quitó (LimpiarEquipo) -> Eliminar.
                implementosDto.Add(new ImplementoUtilizadoModel
                {
                    IntMovimiento = (int)MovimientoItem.Eliminar,
                    IntAGRImplementoUtilizadoKey = _implementoUtilizadoKeyOriginal,
                    IntAGRRegistroActividadLink = ActividadId,
                    IntGENImplementoLink = _implementoLinkOriginal,
                    DecCantidad = 0,
                    VchUsuario = _sesionApp.Login
                });
            }
            // Si no había implemento original y sigue sin haber (EquipoSeleccionado == null), no se manda nada.

            // ── Paso 5: payload completo ──────────────────────────────────
            var payload = new GuardarRegistroActividadPayload
            {
                Descripcion = $"Editar: Preparación de Terreno - {PredioSeleccionado.VchNombre} - {Fecha:dd/MM/yyyy}",
                Actualizacion = actualizarModelo,
                ActividadIdEdicion = ActividadId,
                ActividadRealizadaKeyOriginal = _actividadRealizadaKeyOriginal,
                ActividadRealizada = actividadModelo,
                Insumos = new List<InsumoUtilizadoModel>(),
                Implementos = implementosDto,
                MateriaPrima = new List<MateriaPrimaUtilizadoModel>(),
                ImagenPathLocal = imagenPathLocal,
                ImagenNombre = imagenNombre,
                ImagenUrlRemotaSinCambios = imagenUrlRemotaSinCambios
            };

            var resultado = await _localDataService.GuardarAsync(payload);

            if (resultado.GuardadoEnLinea)
            {
                await Shell.Current.DisplayAlert("Éxito", "Registro actualizado correctamente.", "De acuerdo");
            }
            else if (resultado.Encolado)
            {
                await Shell.Current.DisplayAlert(
                    "Guardado sin conexión",
                    "No hay conexión a internet. Los cambios se guardaron en este dispositivo y se enviarán al servidor cuando sincronices.",
                    "De acuerdo");
            }

            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task Cerrar()
        {
            bool confirmar = await Shell.Current.DisplayAlert(
                "Salir", "¿Deseas salir sin guardar los cambios?", "Salir", "Cancelar");

            if (!confirmar) return;

            BorrarImagenLocalPendiente(ImagenPath);
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task VerDetalle()
        {
            // TODO: navegar a ActividadesRegistradasPrepTerreno (pendiente, igual que en el form general).
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
                Hoy = DateTime.Now.ToString("dd/MM/yyyy");

                EsEdicion = ActividadId > 0;
                TituloPagina = EsEdicion ? "Editar Preparación de Terreno" : "Alta de Preparación de Terreno";

                var tareaVehiculos = _catalogoCacheService.ObtenerAsync("Vehiculos", _actividadService.ObtenerVehiculosAsync);
                var tareaTractores = _catalogoCacheService.ObtenerAsync("TractoresCuadrillas", _actividadService.ObtenerTractoresCuadrillasAsync);
                var tareaJefes = _catalogoCacheService.ObtenerAsync("JefesCuadrilla", _actividadService.ObtenerJefesCuadrillaAsync);
                var tareaProveedores = _catalogoCacheService.ObtenerAsync("Proveedores", _actividadService.ObtenerProveedoresAsync);
                var tareaActividades = _catalogoCacheService.ObtenerAsync("ActividadesCatalogo", _actividadService.ObtenerActividadesCatalogoAsync);
                var tareaEquipos = _catalogoCacheService.ObtenerAsync("Equipos", _actividadService.ObtenerEquiposAsync);

                // Predios: igual que en el form general, se distingue Editar (catálogo completo)
                // de Alta (filtrado por Zona/Municipio).
                var tareaPredios = EsEdicion
                    ? _catalogoCacheService.ObtenerAsync("PrediosCatalogo", _actividadService.ObtenerPrediosCatalogoAsync)
                    : _catalogoCacheService.ObtenerAsync($"PrediosZonaMunicipio_{ZonaId}_{MunicipioId}",
                        ct => _actividadService.ObtenerPrediosAsync(ZonaId, MunicipioId, ct));

                await Task.WhenAll(tareaVehiculos, tareaTractores, tareaJefes, tareaProveedores,
                    tareaActividades, tareaEquipos, tareaPredios);

                Vehiculos = new ObservableCollection<VehiculoActividadModel>(tareaVehiculos.Result);
                TractoresCuadrillas = new ObservableCollection<TractorCuadrillaModel>(tareaTractores.Result);
                JefesCuadrilla = new ObservableCollection<JefeCuadrillaModel>(tareaJefes.Result);
                Proveedores = new ObservableCollection<ProveedorModel>(tareaProveedores.Result);
                Equipos = new ObservableCollection<EquipoModel>(tareaEquipos.Result);
                Predios = new ObservableCollection<PredioModel>(tareaPredios.Result);

                // ── Actividad fija (clave 64) ── ver NOTA al inicio del archivo.
                _actividadFija = tareaActividades.Result.FirstOrDefault(a => a.IntAGRActividadKey == CLAVE_ACTIVIDAD_PREP_TERRENO);

                if (_actividadFija != null)
                {
                    var subactividades = await _catalogoCacheService.ObtenerAsync(
                        $"Subactividades_{_actividadFija.IntAGREtapaKey}_{_actividadFija.IntAGRActividadKey}",
                        ct => _actividadService.ObtenerSubactividadesAsync(_actividadFija.IntAGREtapaKey, _actividadFija.IntAGRActividadKey, ct));
                    Subactividades = new ObservableCollection<SubactividadModel>(subactividades);
                }
                else
                {
                    await Shell.Current.DisplayAlert(
                        "Aviso",
                        "No fue posible localizar la actividad de Preparación de Terreno en el catálogo. Contacta a soporte.",
                        "De acuerdo");
                }

                if (EsEdicion)
                {
                    await CargarRegistroExistenteAsync();
                }
                else
                {
                    PredioSeleccionado = Predios.FirstOrDefault(p => p.IntGENPredioKey == PredioId);

                    // Defaults de Alta: Tipo de cuadrilla = Interna, Estatus = En Proceso.
                    MarcarInterno();
                    MarcarEnProceso();
                }

                // Fire-and-forget: precalienta en caché los Operadores de Maquinaria de TODOS los
                // proveedores (no solo el que el usuario llegue a seleccionar), para que
                // CargarOperadoresAsync ya tenga algo que ofrecer offline aunque el usuario nunca
                // haya visitado ese proveedor específico con conexión. Mismo patrón que
                // PrecalentarCatalogosAsync en RegistroActividadFormViewModel (el general).
                _ = PrecalentarOperadoresAsync();
            }
            catch (Exception ex)
            {
                bool sinDatosLocales = ex is HttpRequestException
                    || ex.InnerException is System.Net.Sockets.SocketException;

                if (sinDatosLocales)
                {
                    await Shell.Current.DisplayAlert(
                        "Sin conexión y sin datos guardados",
                        "No hay conexión a internet y este dispositivo aún no tiene una copia local de los catálogos necesarios para este formulario. Conéctate al menos una vez desde esta pantalla para poder capturar sin conexión más adelante.",
                        "De acuerdo");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error (debug)", ex.ToString(), "De acuerdo");
                }
            }
            finally
            {
                Cargando = false;
            }
        }

        /// <summary>
        /// Carga los datos existentes del registro en modo Edición. Replica CargarRegistroExistenteAsync
        /// del form general, adaptado a los campos propios de Prep. Terreno (Horas/Estatus/Operador/
        /// Código de Unidad) y al implemento único.
        /// </summary>
        private async Task CargarRegistroExistenteAsync()
        {
            if (_registroActividadOriginal is null)
            {
                await Shell.Current.DisplayAlert("Error", "No fue posible cargar la información del registro. Regresa e intenta de nuevo.", "De acuerdo");
                return;
            }

            var reg = _registroActividadOriginal;

            // ── Encabezado ──────────────────────────────────────────────
            Id = reg.VchID;
            Fecha = reg.DtmFecha;
            Observaciones = reg.VchObservacionActividad;
            HorasProductivas = reg.VchHrsProductivasInicial ?? string.Empty; // ver Nota abajo
            HorasMuertas = reg.VchHrsProductivasFinal ?? string.Empty;
            ProduccionInicial = TimeSpan.TryParse(reg.VchHorometroInicial, out var pi) ? pi : TimeSpan.Zero;
            ProduccionFinal = TimeSpan.TryParse(reg.VchHorometroFinal, out var pf) ? pf : TimeSpan.Zero;

            if (reg.VchEstatus == "Terminado")
                MarcarTerminado();
            else
                MarcarEnProceso();

            // Imagen existente (si la hay), mismo patrón que el form general.
            if (!string.IsNullOrEmpty(reg.VchNombreImagen))
            {
                ImagenPath = reg.VchNombreImagen;
                imagenNombre = Path.GetFileName(new Uri(reg.VchNombreImagen).LocalPath);

                _rutaArchivoImagenOriginal = reg.VchNombreImagen;
                _nombreImagenOriginal = imagenNombre;
            }

            PredioSeleccionado = Predios.FirstOrDefault(p => p.IntGENPredioKey == reg.IntGENPredioLink);
            Id = reg.VchID;
            VehiculoSeleccionado = Vehiculos.FirstOrDefault(v => v.IntGENUnidadParaActividadKey == reg.IntGENUnidadParaActividadLink);
            TractorCuadrillaSeleccionado = TractoresCuadrillas.FirstOrDefault(t => t.IntAGRTractoresCuadrillasKey == reg.IntAGRTractoresCuadrillasLink);
            JefeCuadrillaSeleccionado = JefesCuadrilla.FirstOrDefault(j => j.VchNombre == reg.VchJefeCuadrilla);

            CodigoUnidad = reg.VchCodigoUnidad ?? string.Empty;

            if (reg.VchTipoCuadrilla != null && reg.VchTipoCuadrilla.StartsWith("Int", StringComparison.OrdinalIgnoreCase))
            {
                MarcarInterno();
            }
            else
            {
                MarcarExterno();
                ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IntGENProveedorKey == reg.IntGENProveedorLink);

                if (ProveedorSeleccionado != null)
                {
                    await CargarOperadoresAsync(ProveedorSeleccionado.IntGENProveedorKey);

                    OperadorSeleccionado = OperadoresMaquinaria
                        .FirstOrDefault(o => o.IntGENOperadorMaquinariaKey == reg.IntGENOperadorMaquinariaLink);
                }
            }

            // ── Detalle: actividad realizada + implemento ──────────────
            try
            {
                var actividadRealizada = await _actividadService.ObtenerActividadRealizadaAsync(reg.IntAGRRegistroActividadKey, preparacionTerreno: true);
                if (actividadRealizada != null)
                {
                    _actividadRealizadaKeyOriginal = actividadRealizada.IntAGRActividadRealizadaKey;

                    if (_actividadFija != null)
                    {
                        SubactividadSeleccionada = Subactividades.FirstOrDefault(s => s.IntAGRSubActividadKey == actividadRealizada.IntAGRSubActividadLink);
                    }

                    CantidadActividad = actividadRealizada.DecValor.ToString();
                    NoPlantas = actividadRealizada.DecNoPlantas.ToString();
                    NoPersonas = actividadRealizada.DecNoPersonas.ToString();
                    HorasProductivas = actividadRealizada.VchHrsProductivas ?? HorasProductivas;
                    HorasMuertas = actividadRealizada.VchHrsMuertas ?? HorasMuertas;
                    HorasPD = actividadRealizada.DecHorasPD.ToString("0.####");
                }

                // Un solo implemento esperado (regla de negocio: máx. 1 por registro); se toma el primero.
                var implementos = await _actividadService.ObtenerImplementosUtilizadosAsync(reg.IntAGRRegistroActividadKey);
                var implementoExistente = implementos.FirstOrDefault();

                if (implementoExistente != null)
                {
                    _implementoUtilizadoKeyOriginal = implementoExistente.IntAGRImplementoUtilizadoKey;
                    _implementoLinkOriginal = implementoExistente.IntGENImplementoLink;

                    EquipoSeleccionado = Equipos.FirstOrDefault(e => e.IntGENImplementoKey == implementoExistente.IntGENImplementoLink);
                    CantidadEquipo = implementoExistente.DecCantidad.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CargarRegistroExistenteAsync (PrepTerreno) ERROR: {ex}");
                await Shell.Current.DisplayAlert("Advertencia (debug)", ex.ToString(), "De acuerdo");
            }
        }

        // ===================== PRECALENTADO DE CACHÉ OFFLINE =====================

        private const string ClavePreferenciaUltimoPrecalentadoOperadores = "UltimoPrecalentadoOperadoresPrepTerreno";
        private const int DiasEntrePrecalentadosOperadores = 1;

        /// <summary>
        /// Precarga en caché (vía ICatalogoCacheService) los Operadores de Maquinaria de cada
        /// Proveedor ya cargado en Proveedores, para que CargarOperadoresAsync funcione offline
        /// sin depender de que el usuario haya "visitado" ese proveedor antes con conexión.
        ///
        /// Se ejecuta en segundo plano (fire-and-forget) al final de InicializarAsync, con límite
        /// de concurrencia (SemaphoreSlim) y solo una vez por día — mismo patrón que
        /// RegistroActividadFormViewModel.PrecalentarCatalogosAsync (el form general), adaptado
        /// aquí a Operadores de Maquinaria en vez de Subactividades/DetalleInsumo (Prep Terreno no
        /// usa esos dos, ya que la actividad es fija y no maneja insumos).
        /// </summary>
        private async Task PrecalentarOperadoresAsync()
        {
            try
            {
                var ultimaFechaTexto = Preferences.Default.Get(ClavePreferenciaUltimoPrecalentadoOperadores, string.Empty);
                if (DateTime.TryParse(ultimaFechaTexto, out var ultimaFecha) &&
                    (DateTime.Now - ultimaFecha).TotalDays < DiasEntrePrecalentadosOperadores)
                {
                    return; // ya se precalentó recientemente, no repetir
                }

                using var semaforo = new SemaphoreSlim(5);

                var tareas = Proveedores.Select(async proveedor =>
                {
                    await semaforo.WaitAsync();
                    try
                    {
                        await _catalogoCacheService.ObtenerAsync(
                            $"OperadoresMaquinaria_{proveedor.IntGENProveedorKey}",
                            ct => _actividadService.ObtenerOperadoresMaquinariaAsync(proveedor.IntGENProveedorKey, ct));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PrecalentarOperadoresAsync (proveedor {proveedor.IntGENProveedorKey}) error: {ex.Message}");
                    }
                    finally
                    {
                        semaforo.Release();
                    }
                });

                await Task.WhenAll(tareas);

                Preferences.Default.Set(ClavePreferenciaUltimoPrecalentadoOperadores, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                // El precalentado es una optimización silenciosa: si falla (p. ej. se corta la
                // conexión a la mitad), no se le notifica al usuario ni se rompe el formulario.
                System.Diagnostics.Debug.WriteLine($"PrecalentarOperadoresAsync error general: {ex}");
            }
        }
    }
}
