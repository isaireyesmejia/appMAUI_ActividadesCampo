using agaverosActividades.Models;
using agaverosActividades.Models.Actividades;
using agaverosActividades.Models.Catalogos;
using agaverosActividades.Services;
using agaverosActividades.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace agaverosActividades.ViewModels
{
    /// <summary>
    /// Representa un insumo/preparación ya agregado en pantalla, pendiente de guardar.
    /// Es un modelo de UI (no viaja al API tal cual) que junto con IntAGRActividadLink
    /// permite construir el InsumoUtilizadoModel real al presionar Guardar.
    /// </summary>
    /// 
    public enum MovimientoItem
    {
        Nada = 0,
        Agregar = 1,
        Actualizar = 2,
        Eliminar = 3
    }

    public partial class InsumoAgregadoItem : ObservableObject
    {
        public int IntAGRInsumoUtilizadoKey { get; set; }
        public int IntAGRMastInsumoLink { get; set; }
        public int IntAGRActividadLink { get; set; }
        public MovimientoItem Movimiento { get; set; } = MovimientoItem.Agregar;

        [ObservableProperty]
        private string descripcion = string.Empty;

        [ObservableProperty]
        private decimal cantidad;

        [ObservableProperty]
        private string unidad = string.Empty;

        [ObservableProperty]
        private string noValeSalida = string.Empty;

        [ObservableProperty]
        private bool esReceta;

        /// <summary>
        /// PASO 7: si el usuario modifica la cantidad de un insumo que ya existía en el
        /// servidor (Movimiento == Nada, cargado desde CargarRegistroExistenteAsync),
        /// se marca automáticamente como Actualizar para que Guardar() lo mande con el
        /// IntMovimiento correcto. Si ya estaba marcado como Agregar (item nuevo de esta
        /// sesión) o Eliminar, no se toca: un insumo nuevo se sigue mandando como Agregar.
        /// </summary>
        partial void OnCantidadChanged(decimal value)
        {
            if (Movimiento == MovimientoItem.Nada)
                Movimiento = MovimientoItem.Actualizar;
        }
    }
    public partial class MateriaPrimaAgregadaItem : ObservableObject
    {
        public int IntAGRMateriaPrimaUtilizadaKey { get; set; }
        public int IntAGRMateriaPrimaLink { get; set; }
        public int IntAGRMastInsumoLink { get; set; } // liga con el insumo que la generó
        public int IntAGRActividadLink { get; set; }

        [ObservableProperty]
        private string descripcion = string.Empty;

        [ObservableProperty]
        private decimal cantidad;

        [ObservableProperty]
        private string unidad = string.Empty;

        [ObservableProperty]
        private decimal cantidadOriginal;
    }
    public partial class ImplementoAgregadoItem : ObservableObject
    {
        public int IntAGRImplementoUtilizadoKey { get; set; }
        public int IntGENImplementoLink { get; set; }
        public MovimientoItem Movimiento { get; set; } = MovimientoItem.Agregar;

        [ObservableProperty]
        private string descripcion = string.Empty;

        [ObservableProperty]
        private decimal cantidad;

        /// <summary>PASO 7: mismo criterio que en InsumoAgregadoItem.OnCantidadChanged.</summary>
        partial void OnCantidadChanged(decimal value)
        {
            if (Movimiento == MovimientoItem.Nada)
                Movimiento = MovimientoItem.Actualizar;
        }
    }

    /// <summary>
    /// ViewModel de la página unificada de Alta/Edición de Registro de Actividad
    /// (RegistroActividadFormPage). Se navega en modo Alta con idPredio/idZona/idMunicipio,
    /// o en modo Editar con idActividad (ver RegistroActividadesViewModel.AgregarAsync/EditarAsync).
    ///
    /// PASADA 3: se implementa el guardado en modo Edición (Guardar() bifurcado en
    /// GuardarAltaAsync/GuardarEdicionAsync), incluyendo marcado de insumos/implementos
    /// eliminados (Movimiento = Eliminar) y actualizados (Movimiento = Actualizar).
    ///
    /// PASADA 4: en modo Edición, al cargar el registro existente ahora también se recalcula
    /// la materia prima explotada de cada insumo ya guardado (con la cantidad tal cual está en
    /// servidor), para que "Ver materia prima ›" funcione igual que en Alta.
    ///
    /// PASADA 6: el Picker nativo de "Preparación" se reemplaza por SearchablePickerPage
    /// (selector modal con buscador), ya que el catálogo de Preparaciones puede tener
    /// cientos de elementos y el Picker nativo no permite filtrar. Ver SeleccionarPreparacion().
    ///
    /// Pendiente todavía:
    /// - Paso 9: bug de binding AncestorType en los botones de eliminar del CollectionView.
    /// - Fallback Offline (SQLite), se agrega al final del proyecto.
    /// </summary>
    [QueryProperty(nameof(PredioId), "idPredio")]
    [QueryProperty(nameof(ZonaId), "idZona")]
    [QueryProperty(nameof(MunicipioId), "idMunicipio")]
    [QueryProperty(nameof(ActividadId), "idActividad")]
    public partial class RegistroActividadFormViewModel : ObservableObject, IQueryAttributable
    {

        private readonly IActividadService _actividadService;
        private readonly ISesionApp _sesionApp;
        private readonly IMediaService _mediaService;
        private readonly ILocalDataService _localDataService;
        private readonly ICatalogoCacheService _catalogoCacheService;
        private RegistroActividadModel? _registroActividadOriginal;

        /// <summary>
        /// PASO 8: key real de AGRActividadRealizada en el servidor, capturada en
        /// CargarRegistroExistenteAsync. Se necesita en GuardarEdicionAsync para mandar
        /// IntMovimiento = Actualizar con la key correcta (si se manda 0, el backend
        /// generaría un registro de actividad realizada nuevo en vez de actualizar el existente).
        /// AJUSTAR el nombre de la propiedad si el modelo devuelto por
        /// ObtenerActividadRealizadaAsync usa otro nombre de campo para esta key.
        /// </summary>
        private int _actividadRealizadaKeyOriginal;

        /// <summary>PASO 7: insumos existentes en servidor marcados para eliminar (Movimiento = Eliminar),
        /// removidos de la UI (InsumosAgregados) pero conservados aquí para que Guardar() los mande.</summary>
        private readonly List<InsumoAgregadoItem> _insumosEliminados = new();

        /// <summary>PASO 7: mismo propósito que _insumosEliminados, para implementos.</summary>
        private readonly List<ImplementoAgregadoItem> _implementosEliminados = new();

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("registroActividad", out var value) && value is RegistroActividadModel modelo)
                _registroActividadOriginal = modelo;
        }

        public RegistroActividadFormViewModel(
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

        /// <summary>Predio preseleccionado (modo Alta). 0 si se llegó en modo Editar.</summary>
        [ObservableProperty]
        private int predioId;

        /// <summary>Zona preseleccionada (modo Alta), necesaria para ObtenerPrediosAsync.</summary>
        [ObservableProperty]
        private int zonaId;

        /// <summary>Municipio preseleccionado (modo Alta), necesario para ObtenerPrediosAsync.</summary>
        [ObservableProperty]
        private int municipioId;

        /// <summary>Si viene con valor (> 0), la página abre en modo Editar y carga el registro existente.</summary>
        [ObservableProperty]
        private int actividadId;

        // ===================== ENCABEZADO =====================

        [ObservableProperty]
        private string tituloPagina = "Alta de Registro de Actividad";

        [ObservableProperty]
        private string usuario;

        [ObservableProperty]
        private string perfil;

        [ObservableProperty]
        private string hoy;

        [ObservableProperty]
        private bool cargando;

        [ObservableProperty]
        private bool frameDetallesVisible;

        // ===================== DATOS GENERALES =====================

        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private DateTime fecha = DateTime.Now;

        /// <summary>True cuando el formulario permite editar Predio/Fecha (pantalla de Edición); false en Alta.</summary>
        [ObservableProperty]
        private bool esEdicion;

        [ObservableProperty]
        private ObservableCollection<PredioModel> predios = new();

        [ObservableProperty]
        private PredioModel predioSeleccionado;

        partial void OnPredioSeleccionadoChanged(PredioModel value)
        {
            Id = value != null ? $"{value.VchCodigo}-0000" : string.Empty;
        }

        [ObservableProperty]
        private ObservableCollection<VehiculoActividadModel> vehiculos = new();

        [ObservableProperty]
        private VehiculoActividadModel vehiculoSeleccionado;

        [ObservableProperty]
        private ObservableCollection<TractorCuadrillaModel> tractoresCuadrillas = new();

        [ObservableProperty]
        private TractorCuadrillaModel tractorCuadrillaSeleccionado;

        /// <summary>Controla si se muestran horómetro y producción (solo para "Tractor" o "Maquinaria").</summary>
        [ObservableProperty]
        private bool camposHorometroHabilitados;

        partial void OnTractorCuadrillaSeleccionadoChanged(TractorCuadrillaModel value)
        {
            bool esTractorOMaquinaria = value != null &&
                (value.VchDescripcion == "Tractor" || value.VchDescripcion == "Maquinaria");

            CamposHorometroHabilitados = esTractorOMaquinaria;

            if (!esTractorOMaquinaria)
            {
                Inicial = "0";
                Final = "0";
                ProduccionInicial = TimeSpan.Zero;
                ProduccionFinal = TimeSpan.Zero;
            }
        }

        [ObservableProperty]
        private ObservableCollection<JefeCuadrillaModel> jefesCuadrilla = new();

        [ObservableProperty]
        private JefeCuadrillaModel jefeCuadrillaSeleccionado;

        // ===================== TIPO DE CUADRILLA (Interno/Externo) =====================

        [ObservableProperty]
        private bool esInterno;

        [ObservableProperty]
        private bool esExterno;

        [ObservableProperty]
        private bool proveedorHabilitado;

        [ObservableProperty]
        private ObservableCollection<ProveedorModel> proveedores = new();

        [ObservableProperty]
        private ProveedorModel proveedorSeleccionado;

        [RelayCommand]
        private void MarcarInterno()
        {
            EsInterno = true;
            EsExterno = false;
            ProveedorHabilitado = false;
            ProveedorSeleccionado = null;
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
            // Igual que en Xamarin: si se elige un proveedor, se marca automáticamente "Externo".
            if (value != null && !EsExterno)
            {
                MarcarExterno();
            }
        }

        // ===================== ACTIVIDAD =====================

        [ObservableProperty]
        private ObservableCollection<ActividadAgricolaModel> actividades = new();

        [ObservableProperty]
        private ActividadAgricolaModel actividadSeleccionada;

        [ObservableProperty]
        private ObservableCollection<SubactividadModel> subactividades = new();

        [ObservableProperty]
        private SubactividadModel subactividadSeleccionada;

        [ObservableProperty]
        private string cantidadActividad;

        [ObservableProperty]
        private string unidadActividad;

        [ObservableProperty]
        private string noPlantas;

        [ObservableProperty]
        private string noPersonas;

        [ObservableProperty]
        private bool soloActividad;

        partial void OnSoloActividadChanged(bool value)
        {
            if (value)
            {
                // Si es "Solo Actividad" no se requiere materia prima: se limpia la sección.
                PreparacionSeleccionada = null;
                CantidadPreparacion = "0";
            }
        }

        partial void OnActividadSeleccionadaChanged(ActividadAgricolaModel value)
        {
            Subactividades.Clear();
            SubactividadSeleccionada = null;
            UnidadActividad = string.Empty;

            if (value != null)
                _ = CargarSubactividadesAsync(value.IntAGREtapaKey, value.IntAGRActividadKey);
        }

        private async Task CargarSubactividadesAsync(int etapaKey, int actividadKey)
        {
            try
            {
                var lista = await _catalogoCacheService.ObtenerAsync(
                    $"Subactividades_{etapaKey}_{actividadKey}",
                    ct => _actividadService.ObtenerSubactividadesAsync(etapaKey, actividadKey, ct));
                Subactividades = new ObservableCollection<SubactividadModel>(lista);
            }
            catch (Exception ex)
            {
                var mensaje = EsFallaSinCache(ex)
                    ? "No hay conexión y esta actividad no se ha consultado antes sin internet, así que no hay subactividades guardadas localmente. Conéctate una vez para poder usarla offline."
                    : "No se pudieron cargar las subactividades.";
                await Shell.Current.DisplayAlert("Aviso", mensaje, "De acuerdo");
            }
        }

        partial void OnSubactividadSeleccionadaChanged(SubactividadModel value)
        {
            UnidadActividad = value?.VchUnidadDeMedida ?? string.Empty;
        }

        // ===================== MATERIA PRIMA / PREPARACIÓN =====================

        [ObservableProperty]
        private ObservableCollection<ClasificacionInsumoModel> clasificaciones = new();

        [ObservableProperty]
        private ClasificacionInsumoModel clasificacionSeleccionada;

        [ObservableProperty]
        private ObservableCollection<PreparacionModel> preparaciones = new();

        [ObservableProperty]
        private PreparacionModel preparacionSeleccionada;

        [ObservableProperty]
        private string lblPreparacionSeleccionada;

        [ObservableProperty]
        private string cantidadPreparacion;

        [ObservableProperty]
        private string unidadPreparacion;

        [ObservableProperty]
        private string tipoPreparacion;

        [ObservableProperty]
        private string noValeSalida;

        [ObservableProperty]
        private bool noValeSalidaHabilitado = true;

        private async Task CargarPreparacionesAsync()
        {
            try
            {
                var lista = await _actividadService.ObtenerPreparacionesAsync();
                Preparaciones = new ObservableCollection<PreparacionModel>(lista);
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudieron cargar las preparaciones.", "De acuerdo");
            }
        }

        /// <summary>
        /// PASADA 6: abre el selector con buscador (SearchablePickerPage) en vez del Picker
        /// nativo, ya que el catálogo de Preparaciones puede tener cientos de elementos
        /// (~380 en producción, ver PrecalentarCatalogosAsync) y elegir a mano scrolleando
        /// una lista tan larga es una mala experiencia. Al elegir, se asigna
        /// PreparacionSeleccionada normalmente, disparando OnPreparacionSeleccionadaChanged
        /// igual que si hubiera venido del Picker.
        /// </summary>
        [RelayCommand]
        private async Task SeleccionarPreparacion()
        {
            var seleccion = await SearchablePickerPage.MostrarAsync(
                titulo: "Preparación",
                items: Preparaciones,
                textoMostrar: p => $"{p.VchClave} - {p.VchNombreComun}");

            if (seleccion != null)
                PreparacionSeleccionada = seleccion;
        }

        partial void OnPreparacionSeleccionadaChanged(PreparacionModel value)
        {
            if (value == null)
            {
                LblPreparacionSeleccionada = string.Empty;
                UnidadPreparacion = string.Empty;
                TipoPreparacion = string.Empty;
                return;
            }

            LblPreparacionSeleccionada = $"{value.VchClave} - {value.VchNombreComun}";
            UnidadPreparacion = value.VchUnidadCosto;
            TipoPreparacion = value.VchTipoPreparacion;
        }

        // ===================== EQUIPO / IMPLEMENTO =====================

        [ObservableProperty]
        private ObservableCollection<EquipoModel> equipos = new();

        [ObservableProperty]
        private EquipoModel equipoSeleccionado;

        [ObservableProperty]
        private string cantidadEquipo = "1";

        [ObservableProperty]
        private ObservableCollection<UnidadEquipoModel> unidadesEquipo = new();

        [ObservableProperty]
        private UnidadEquipoModel unidadEquipoSeleccionada;

        // ===================== LISTAS DE TRABAJO (lo agregado en pantalla, pendiente de guardar) =====================

        [ObservableProperty]
        private ObservableCollection<InsumoAgregadoItem> insumosAgregados = new();
        [ObservableProperty]
        private ObservableCollection<MateriaPrimaAgregadaItem> materiaPrimaAgregada = new();

        [ObservableProperty]
        private ObservableCollection<ImplementoAgregadoItem> implementosAgregados = new();

        /// <summary>
        /// PASO 7: si el insumo ya existe en el servidor (Key > 0), no se borra de verdad:
        /// se marca Movimiento = Eliminar, se quita de la UI (InsumosAgregados) y se conserva
        /// en _insumosEliminados para que Guardar() (modo Edición) lo mande al backend con ese
        /// movimiento. Si es un insumo agregado en esta misma sesión (Key == 0, nunca existió
        /// en servidor), se elimina directamente sin dejar rastro.
        /// </summary>
        [RelayCommand]
        private void QuitarInsumo(InsumoAgregadoItem item)
        {
            if (item == null) return;

            if (item.IntAGRInsumoUtilizadoKey > 0)
            {
                item.Movimiento = MovimientoItem.Eliminar;
                InsumosAgregados.Remove(item); // se quita de la UI...
                _insumosEliminados.Add(item);  // ...pero se conserva para el guardado
            }
            else
            {
                InsumosAgregados.Remove(item);
            }

            var relacionadas = MateriaPrimaAgregada.Where(m => m.IntAGRMastInsumoLink == item.IntAGRMastInsumoLink).ToList();
            foreach (var mp in relacionadas)
                MateriaPrimaAgregada.Remove(mp);
        }

        /// <summary>PASO 7: mismo criterio que QuitarInsumo, para implementos/equipos.</summary>
        [RelayCommand]
        private void QuitarImplemento(ImplementoAgregadoItem item)
        {
            if (item == null) return;

            if (item.IntAGRImplementoUtilizadoKey > 0)
            {
                item.Movimiento = MovimientoItem.Eliminar;
                ImplementosAgregados.Remove(item);
                _implementosEliminados.Add(item);
            }
            else
            {
                ImplementosAgregados.Remove(item);
            }
        }

        [RelayCommand]
        private async Task VerMateriaPrima(InsumoAgregadoItem item)
        {
            if (item == null || !item.EsReceta) return;

            var relacionadas = MateriaPrimaAgregada
                .Where(m => m.IntAGRMastInsumoLink == item.IntAGRMastInsumoLink)
                .ToList();

            if (relacionadas.Count == 0)
            {
                await Shell.Current.DisplayAlert("Aviso", "No hay materia prima calculada para este insumo.", "De acuerdo");
                return;
            }

            var parametros = new Dictionary<string, object>
    {
        { "materiasPrimas", relacionadas },
        { "descripcionInsumo", item.Descripcion }
    };

            await Shell.Current.GoToAsync(nameof(Views.ExplosionRecetaPage), parametros);
        }
        private async Task RecalcularMateriaPrimaAsync(InsumoAgregadoItem insumoItem, int actividadKey, decimal cantidadTotalInsumo, decimal dosisReceta)
        {
            int mastInsumoKey = insumoItem.IntAGRMastInsumoLink;

            var existentes = MateriaPrimaAgregada.Where(m => m.IntAGRMastInsumoLink == mastInsumoKey).ToList();
            foreach (var e in existentes)
                MateriaPrimaAgregada.Remove(e);

            if (dosisReceta <= 0)
            {
                insumoItem.EsReceta = false;
                return;
            }

            try
            {
                var detalle = await _catalogoCacheService.ObtenerAsync(
                    $"DetalleInsumo_{mastInsumoKey}",
                    ct => _actividadService.ObtenerDetalleInsumoAsync(mastInsumoKey, ct));
                var racionAplicada = cantidadTotalInsumo / dosisReceta;

                foreach (var mp in detalle)
                {
                    var cantidadCalculada = mp.DecRequerido * racionAplicada;
                    MateriaPrimaAgregada.Add(new MateriaPrimaAgregadaItem
                    {
                        IntAGRMateriaPrimaLink = mp.IntAGRMateriaPrimaLink,
                        IntAGRMastInsumoLink = mastInsumoKey,
                        IntAGRActividadLink = actividadKey,
                        Descripcion = mp.VchDescripcionMateriaPrima,
                        CantidadOriginal = cantidadCalculada,
                        Cantidad = cantidadCalculada,
                        Unidad = mp.VchUnidadMateriaPrima
                    });
                }

                insumoItem.EsReceta = detalle.Any();
            }
            catch (Exception)
            {
                insumoItem.EsReceta = false;
                await Shell.Current.DisplayAlert("Advertencia", "No fue posible calcular la materia prima requerida.", "De acuerdo");
            }
        }


        /// <summary>
        /// PASADA 4: variante silenciosa de RecalcularMateriaPrimaAsync, usada exclusivamente al
        /// cargar un registro existente (CargarRegistroExistenteAsync). No muestra DisplayAlert si
        /// falla la llamada al backend, para no interrumpir la carga inicial de la pantalla con un
        /// insumo por insumo; en su lugar simplemente deja EsReceta = false para ese insumo (el
        /// usuario no verá el link "Ver materia prima ›" pero el resto del formulario carga normal).
        /// </summary>
        private async Task RecalcularMateriaPrimaSilenciosoAsync(InsumoAgregadoItem insumoItem, int actividadKey, decimal cantidadTotalInsumo, decimal dosisReceta)
        {
            int mastInsumoKey = insumoItem.IntAGRMastInsumoLink;

            if (dosisReceta <= 0)
            {
                insumoItem.EsReceta = false;
                return;
            }

            try
            {
                var detalle = await _catalogoCacheService.ObtenerAsync(
                    $"DetalleInsumo_{mastInsumoKey}",
                    ct => _actividadService.ObtenerDetalleInsumoAsync(mastInsumoKey, ct));
                var racionAplicada = cantidadTotalInsumo / dosisReceta;
                // ... resto igual

                foreach (var mp in detalle)
                {
                    var cantidadCalculada = mp.DecRequerido * racionAplicada;
                    MateriaPrimaAgregada.Add(new MateriaPrimaAgregadaItem
                    {
                        IntAGRMateriaPrimaLink = mp.IntAGRMateriaPrimaLink,
                        IntAGRMastInsumoLink = mastInsumoKey,
                        IntAGRActividadLink = actividadKey,
                        Descripcion = mp.VchDescripcionMateriaPrima,
                        CantidadOriginal = cantidadCalculada,
                        Cantidad = cantidadCalculada,
                        Unidad = mp.VchUnidadMateriaPrima
                    });
                }

                insumoItem.EsReceta = detalle.Any();
            }
            catch (Exception ex)
            {
                insumoItem.EsReceta = false;
                var mensaje = EsFallaSinCache(ex)
                    ? "No hay conexión y este insumo no se ha usado antes sin internet, así que no se puede calcular su materia prima ahora. El insumo se guardará, pero sin ese detalle."
                    : "No fue posible calcular la materia prima requerida.";
                await Shell.Current.DisplayAlert("Advertencia", mensaje, "De acuerdo");
            }
        }

        // ===================== HORÓMETRO / PRODUCCIÓN =====================

        [ObservableProperty]
        private string inicial = "0";

        [ObservableProperty]
        private string final = "0";

        [ObservableProperty]
        private TimeSpan produccionInicial;

        [ObservableProperty]
        private TimeSpan produccionFinal;

        // ===================== IMAGEN / OBSERVACIONES =====================

        [ObservableProperty]
        private string observaciones;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HayImagen))]
        private string? imagenPath;

        private string? imagenNombre;

        /// <summary>
        /// PASADA 5: referencia (URL/ruta) de la imagen tal cual venía del servidor al entrar en
        /// modo Edición (CargarRegistroExistenteAsync). Se usa en GuardarEdicionAsync para saber
        /// si el usuario dejó la imagen intacta (en cuyo caso se reenvía esta misma referencia sin
        /// volver a subir nada) o la reemplazó por una nueva (ahí sí se sube). Queda en null en
        /// modo Alta, donde no aplica.
        /// </summary>
        private string? _rutaArchivoImagenOriginal;
        private string? _nombreImagenOriginal;

        /// <summary>PASADA 5: true si path es una URL absoluta http/https (imagen ya guardada en servidor).</summary>
        private static bool EsImagenRemota(string? path) =>
            !string.IsNullOrEmpty(path) &&
            Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "http" || uri.Scheme == "https");

        /// <summary>True cuando ya hay una imagen capturada/seleccionada. Controla qué bloque de UI se muestra
        /// (botón "Tomar foto" vs. miniatura + Ver/Reemplazar/Quitar).</summary>
        public bool HayImagen => !string.IsNullOrEmpty(ImagenPath);

        /// <summary>Se dispara para pedir a la página que muestre la imagen en pantalla completa (Ver imagen).</summary>
        public event Func<string, Task>? SolicitarMostrarImagen;

        /// <summary>
        /// Borra el archivo local de imagen pendiente (capturada/elegida pero aún no guardada),
        /// si existe. No borra si ImagenPath es una URL remota (imagen ya guardada en servidor,
        /// modo Editar) — solo limpia archivos locales generados por MediaService.
        /// </summary>
        private void BorrarImagenLocalPendiente(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                return; // es una URL de servidor, no un archivo local

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                // No bloquea el flujo del usuario; solo se registra para diagnóstico.
                System.Diagnostics.Debug.WriteLine($"BorrarImagenLocalPendiente error: {ex}");
            }
        }

        [RelayCommand]
        private async Task TomarFoto()
        {
            var pathAnterior = ImagenPath;

            try
            {
                var path = await _mediaService.TomarFotoAsync();
                if (path != null)
                {
                    BorrarImagenLocalPendiente(pathAnterior);
                    ImagenPath = path;
                    imagenNombre = Path.GetFileName(path);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error al tomar foto", ex.Message, "De acuerdo");
            }
        }

        [RelayCommand]
        private async Task ElegirDeGaleria()
        {
            var pathAnterior = ImagenPath;

            try
            {
                var path = await _mediaService.ElegirDeGaleriaAsync();
                if (path != null)
                {
                    BorrarImagenLocalPendiente(pathAnterior);
                    ImagenPath = path;
                    imagenNombre = Path.GetFileName(path);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error al elegir imagen", ex.Message, "De acuerdo");
            }
        }

        [RelayCommand]
        private async Task VerImagen()
        {
            if (string.IsNullOrEmpty(ImagenPath) || SolicitarMostrarImagen == null) return;

            try
            {
                await SolicitarMostrarImagen(ImagenPath);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error al mostrar imagen", ex.Message, "De acuerdo");
            }
        }

        [RelayCommand]
        private void QuitarImagen()
        {
            BorrarImagenLocalPendiente(ImagenPath);
            ImagenPath = null;
            imagenNombre = null;
        }

        private async Task<string?> SubirImagenAsync()
        {
            if (string.IsNullOrEmpty(ImagenPath)) return null;

            try
            {
                return await _actividadService.SubirImagenAsync(ImagenPath, imagenNombre ?? Path.GetFileName(ImagenPath));
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Advertencia", "No fue posible subir la imagen, el registro se guardará sin ella.", "De acuerdo");
                return null;
            }
        }

        // ===================== ACCIONES PRINCIPALES =====================

        [ObservableProperty]
        private bool agregarHabilitado = true;

        [ObservableProperty]
        private bool guardarHabilitado = true;

        [RelayCommand]
        private async Task Agregar()
        {
            if (ActividadSeleccionada is null || SubactividadSeleccionada is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona actividad y subactividad.", "Cerrar");
                return;
            }

            bool seAgregoAlgo = false;

            // ---------- PREPARACIÓN / INSUMO ----------
            if (!SoloActividad && PreparacionSeleccionada != null)
            {
                if (!decimal.TryParse(CantidadPreparacion, out var cantidadPrep) || cantidadPrep <= 0)
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Ingresa una cantidad válida de preparación.", "Cerrar");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NoValeSalida))
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Ingresa el número de vale de salida.", "Cerrar");
                    return;
                }

                var insumoExistente = InsumosAgregados
                    .FirstOrDefault(i => i.IntAGRMastInsumoLink == PreparacionSeleccionada.IntAGRMastInsumoKey);

                if (insumoExistente != null)
                {
                    insumoExistente.Cantidad += cantidadPrep;
                }
                else
                {
                    InsumosAgregados.Add(new InsumoAgregadoItem
                    {
                        IntAGRMastInsumoLink = PreparacionSeleccionada.IntAGRMastInsumoKey,
                        IntAGRActividadLink = ActividadSeleccionada.IntAGRActividadKey,
                        Descripcion = $"{PreparacionSeleccionada.VchClave} - {PreparacionSeleccionada.VchNombreComun}",
                        Cantidad = cantidadPrep,
                        Unidad = UnidadPreparacion,
                        NoValeSalida = NoValeSalida
                    });
                }

                // Una vez que hay al menos un insumo, el vale de salida ya no debe modificarse.
                NoValeSalidaHabilitado = false;

                var itemAfectado = insumoExistente ?? InsumosAgregados.Last(); // el que se acaba de agregar
                var cantidadTotalInsumo = insumoExistente != null ? insumoExistente.Cantidad : cantidadPrep;
                await RecalcularMateriaPrimaAsync(
                    itemAfectado,
                    ActividadSeleccionada.IntAGRActividadKey,
                    cantidadTotalInsumo,
                    PreparacionSeleccionada.DecDosis);

                PreparacionSeleccionada = null;
                CantidadPreparacion = string.Empty;
                seAgregoAlgo = true;
            }

            // ---------- EQUIPO / IMPLEMENTO ----------
            if (EquipoSeleccionado != null)
            {
                if (!decimal.TryParse(CantidadEquipo, out var cantidadEquipo) || cantidadEquipo <= 0)
                {
                    await Shell.Current.DisplayAlert("Advertencia", "Ingresa una cantidad válida de equipo.", "Cerrar");
                    return;
                }

                var implementoExistente = ImplementosAgregados
                    .FirstOrDefault(i => i.IntGENImplementoLink == EquipoSeleccionado.IntGENImplementoKey);

                if (implementoExistente != null)
                {
                    implementoExistente.Cantidad += cantidadEquipo;
                }
                else
                {
                    ImplementosAgregados.Add(new ImplementoAgregadoItem
                    {
                        IntGENImplementoLink = EquipoSeleccionado.IntGENImplementoKey,
                        Descripcion = EquipoSeleccionado.VchNombreConCodigo,
                        Cantidad = cantidadEquipo
                    });
                }

                EquipoSeleccionado = null;
                CantidadEquipo = "1";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UnidadEquipoSeleccionada = null;
                });
                seAgregoAlgo = true;
            }

            if (!seAgregoAlgo)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona una preparación o un equipo para agregar.", "Cerrar");
            }
        }

        /// <summary>
        /// PASO 8: punto de entrada único de guardado. Hace las validaciones comunes a
        /// Alta y Edición, y luego bifurca según EsEdicion. Antes, este método siempre
        /// llamaba al flujo de Alta (AltaRegistroActividadAsync), por lo que editar y
        /// guardar terminaba creando un registro nuevo en vez de actualizar el existente.
        /// </summary>
        [RelayCommand]
        private async Task Guardar()
        {
            if (ActividadSeleccionada is null || SubactividadSeleccionada is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona actividad y subactividad.", "De acuerdo");
                return;
            }

            if (VehiculoSeleccionado is null || TractorCuadrillaSeleccionado is null || JefeCuadrillaSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Completa unidad, cuadrilla/tractor y jefe de cuadrilla.", "De acuerdo");
                return;
            }

            if (EsExterno && ProveedorSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Selecciona un proveedor para cuadrilla externa.", "De acuerdo");
                return;
            }

            if (PredioSeleccionado is null)
            {
                await Shell.Current.DisplayAlert("Advertencia", "No hay predio seleccionado.", "De acuerdo");
                return;
            }

            if (!decimal.TryParse(CantidadActividad, out var cantidadActividad))
            {
                await Shell.Current.DisplayAlert("Advertencia", "La cantidad de actividad no es válida.", "De acuerdo");
                return;
            }

            // Igual que en Xamarin: si no es "Solo Actividad", se requiere al menos un insumo agregado.
            if (!SoloActividad && InsumosAgregados.Count == 0)
            {
                await Shell.Current.DisplayAlert("Advertencia", "Es necesario agregar una materia prima (botón Agregar).", "De acuerdo");
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
                // Mensaje de error de negocio devuelto por el backend (BadRequest).
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
            // ── Paso 1: armar el encabezado (igual que antes, pero ya NO se envía aquí) ──
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
                VchEstatus = "En Proceso",
                VchHrsProductivasInicial = ProduccionInicial.ToString(@"hh\:mm"),
                VchHrsProductivasFinal = ProduccionFinal.ToString(@"hh\:mm"),
                VchHorometroInicial = Inicial,
                VchHorometroFinal = Final,
                IntGENProveedorLink = ProveedorSeleccionado?.IntGENProveedorKey ?? -1,
                IntGENOperadorMaquinariaLink = null,
                VchCodigoUnidad = string.Empty
            };

            // ── Paso 2: armar la actividad realizada (sin registroActividadKey todavía;
            //    LocalDataService lo llena internamente al ejecutar el payload) ──────
            var horasProductivas = (ProduccionFinal - ProduccionInicial).ToString(@"hh\:mm");

            var actividadModelo = new ActividadRealizadaModel
            {
                IntMovimiento = 1,
                IntAGRActividadRealizadaKey = 0,
                IntAGRRegistroActividadLink = 0, // lo resuelve LocalDataService tras el alta
                IntAGRActividadLink = ActividadSeleccionada.IntAGRActividadKey,
                IntAGRSubActividadLink = SubactividadSeleccionada.IntAGRSubActividadKey,
                DecValor = cantidadActividad,
                DecNoPlantas = decimal.TryParse(NoPlantas, out var noPlantas) ? noPlantas : 0,
                DecNoPersonas = decimal.TryParse(NoPersonas, out var noPersonas) ? noPersonas : 0,
                VchObservaciones = Observaciones ?? string.Empty,
                VchHrsProductivas = horasProductivas,
                VchHrsMuertas = string.Empty,
                DecHorasPD = 0,
                VchNombreImagen = string.Empty, // lo resuelve LocalDataService al subir/reenviar la imagen
                VchRutaArchivo = string.Empty,
                VchUsuario = _sesionApp.Login
            };

            // ── Paso 3: armar insumos/implementos/materia prima (sin registroActividadKey aún) ──
            var insumosDto = InsumosAgregados.Select(insumo => new InsumoUtilizadoModel
            {
                IntMovimiento = 1,
                IntAGRInsumoUtilizadoKey = 0,
                IntAGRRegistroActividadLink = 0,
                IntAGRMastInsumoLink = insumo.IntAGRMastInsumoLink,
                IntAGRActividadLink = insumo.IntAGRActividadLink,
                DecValor = insumo.Cantidad,
                VchObservaciones = string.Empty,
                VchNoValeDeSalida = insumo.NoValeSalida,
                VchUsuario = _sesionApp.Login
            }).ToList();

            var implementosDto = ImplementosAgregados.Select(implemento => new ImplementoUtilizadoModel
            {
                IntMovimiento = 1,
                IntAGRImplementoUtilizadoKey = 0,
                IntAGRRegistroActividadLink = 0,
                IntGENImplementoLink = implemento.IntGENImplementoLink,
                DecCantidad = implemento.Cantidad,
                VchUsuario = _sesionApp.Login
            }).ToList();

            var materiaPrimaDto = MateriaPrimaAgregada.Select(materiaPrima => new MateriaPrimaUtilizadoModel
            {
                IntMovimiento = MateriaPrimaUtilizadoModel.MovimientoMP.Agregar,
                IntAGRMateriaPrimaUtilizadaKey = 0,
                IntAGRRegistroActividadLink = 0,
                IntAGRMateriaPrimaLink = materiaPrima.IntAGRMateriaPrimaLink,
                IntAGRMastInsumoLink = materiaPrima.IntAGRMastInsumoLink,
                IntAGRActividadLink = materiaPrima.IntAGRActividadLink,
                DecValor = materiaPrima.Cantidad,
                VchObservaciones = string.Empty,
                VchUsuario = _sesionApp.Login
            }).ToList();

            // ── Paso 4: armar el payload completo y delegarlo a ILocalDataService ──────
            var payload = new GuardarRegistroActividadPayload
            {
                Descripcion = $"{ActividadSeleccionada.VchDescripcion} - {PredioSeleccionado.VchNombre} - {Fecha:dd/MM/yyyy}",
                Alta = altaModelo,
                ActividadRealizada = actividadModelo,
                Insumos = insumosDto,
                Implementos = implementosDto,
                MateriaPrima = materiaPrimaDto,
                ImagenPathLocal = !string.IsNullOrEmpty(ImagenPath) && !EsImagenRemota(ImagenPath) ? ImagenPath : null,
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

        private async Task GuardarEdicionAsync(decimal cantidadActividad)
        {
            // ── Paso 1: armar el encabezado a actualizar (igual que antes) ──────────────
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
                VchEstatus = "En Proceso",
                VchHrsProductivasInicial = ProduccionInicial.ToString(@"hh\:mm"),
                VchHrsProductivasFinal = ProduccionFinal.ToString(@"hh\:mm"),
                VchHorometroInicial = Inicial,
                VchHorometroFinal = Final,
                IntGENProveedorLink = ProveedorSeleccionado?.IntGENProveedorKey
            };

            // ── Paso 2: resolver qué pasa con la imagen (misma lógica de 3 casos de antes,
            //    pero en vez de subir aquí, solo se decide QUÉ referencia mandar al payload) ──
            string? imagenPathLocal = null;
            string? imagenUrlRemotaSinCambios = null;

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

            // ── Paso 3: armar la actividad realizada (Actualizar, con la key existente) ──
            var horasProductivas = (ProduccionFinal - ProduccionInicial).ToString(@"hh\:mm");

            var actividadModelo = new ActividadRealizadaModel
            {
                IntMovimiento = 2,
                IntAGRActividadRealizadaKey = _actividadRealizadaKeyOriginal,
                IntAGRRegistroActividadLink = ActividadId,
                IntAGRActividadLink = ActividadSeleccionada.IntAGRActividadKey,
                IntAGRSubActividadLink = SubactividadSeleccionada.IntAGRSubActividadKey,
                DecValor = cantidadActividad,
                DecNoPlantas = decimal.TryParse(NoPlantas, out var noPlantas) ? noPlantas : 0,
                DecNoPersonas = decimal.TryParse(NoPersonas, out var noPersonas) ? noPersonas : 0,
                VchObservaciones = Observaciones ?? string.Empty,
                VchHrsProductivas = horasProductivas,
                VchHrsMuertas = string.Empty,
                DecHorasPD = 0,
                VchNombreImagen = string.Empty, // lo resuelve LocalDataService
                VchRutaArchivo = string.Empty,  // lo resuelve LocalDataService
                VchUsuario = _sesionApp.Login
            };

            // ── Paso 4 (+ Paso 7 original): Insumos — Agregar / Actualizar / Eliminar ──
            var todosInsumos = InsumosAgregados
                .Concat(_insumosEliminados)
                .Where(i => i.Movimiento != MovimientoItem.Nada)
                .Select(insumo => new InsumoUtilizadoModel
                {
                    IntMovimiento = (int)insumo.Movimiento,
                    IntAGRInsumoUtilizadoKey = insumo.IntAGRInsumoUtilizadoKey,
                    IntAGRRegistroActividadLink = ActividadId,
                    IntAGRMastInsumoLink = insumo.IntAGRMastInsumoLink,
                    IntAGRActividadLink = insumo.IntAGRActividadLink,
                    DecValor = insumo.Cantidad,
                    VchObservaciones = string.Empty,
                    VchNoValeDeSalida = insumo.NoValeSalida,
                    VchUsuario = _sesionApp.Login
                }).ToList();

            // ── Paso 5 (+ Paso 7 original): Implementos — Agregar / Actualizar / Eliminar ──
            var todosImplementos = ImplementosAgregados
                .Concat(_implementosEliminados)
                .Where(i => i.Movimiento != MovimientoItem.Nada)
                .Select(implemento => new ImplementoUtilizadoModel
                {
                    IntMovimiento = (int)implemento.Movimiento,
                    IntAGRImplementoUtilizadoKey = implemento.IntAGRImplementoUtilizadoKey,
                    IntAGRRegistroActividadLink = ActividadId,
                    IntGENImplementoLink = implemento.IntGENImplementoLink,
                    DecCantidad = implemento.Cantidad,
                    VchUsuario = _sesionApp.Login
                }).ToList();

            // ── Paso 6: Materia prima explotada ──────────────────────────
            var materiaPrimaDto = MateriaPrimaAgregada.Select(materiaPrima => new MateriaPrimaUtilizadoModel
            {
                IntMovimiento = MateriaPrimaUtilizadoModel.MovimientoMP.Agregar,
                IntAGRMateriaPrimaUtilizadaKey = 0,
                IntAGRRegistroActividadLink = ActividadId,
                IntAGRMateriaPrimaLink = materiaPrima.IntAGRMateriaPrimaLink,
                IntAGRMastInsumoLink = materiaPrima.IntAGRMastInsumoLink,
                IntAGRActividadLink = materiaPrima.IntAGRActividadLink,
                DecValor = materiaPrima.Cantidad,
                VchObservaciones = string.Empty,
                VchUsuario = _sesionApp.Login
            }).ToList();

            // ── Paso 7: armar el payload completo y delegarlo a ILocalDataService ──────
            var payload = new GuardarRegistroActividadPayload
            {
                Descripcion = $"Editar: {ActividadSeleccionada.VchDescripcion} - {PredioSeleccionado.VchNombre} - {Fecha:dd/MM/yyyy}",
                Actualizacion = actualizarModelo,
                ActividadIdEdicion = ActividadId,
                ActividadRealizadaKeyOriginal = _actividadRealizadaKeyOriginal,
                ActividadRealizada = actividadModelo,
                Insumos = todosInsumos,
                Implementos = todosImplementos,
                MateriaPrima = materiaPrimaDto,
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
            // TODO (pasada 2): navegar a ActividadesRegistradas.
        }

        // ===================== INICIALIZACIÓN =====================

        /// <summary>
        /// Se llama desde OnAppearing() de la página. Carga catálogos y, según los parámetros de
        /// navegación recibidos, arranca en modo Alta (con Predio/Zona/Municipio preseleccionados)
        /// o en modo Editar (con ActividadId, carga de datos existentes pendiente).
        /// </summary>
        private bool _yaInicializado;

        /// <summary>
        /// Distingue "sin conexión y sin copia en caché" de un error real de programación/backend,
        /// para mostrar mensajes distintos al usuario. Misma lógica que ya se usa en InicializarAsync.
        /// </summary>
        private static bool EsFallaSinCache(Exception ex) =>
            ex is HttpRequestException || ex.InnerException is System.Net.Sockets.SocketException;

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
                TituloPagina = EsEdicion ? "Editar Registro de Actividad" : "Alta de Registro de Actividad";

                var tareaVehiculos = _catalogoCacheService.ObtenerAsync("Vehiculos", _actividadService.ObtenerVehiculosAsync);
                var tareaTractores = _catalogoCacheService.ObtenerAsync("TractoresCuadrillas", _actividadService.ObtenerTractoresCuadrillasAsync);
                var tareaJefes = _catalogoCacheService.ObtenerAsync("JefesCuadrilla", _actividadService.ObtenerJefesCuadrillaAsync);
                var tareaProveedores = _catalogoCacheService.ObtenerAsync("Proveedores", _actividadService.ObtenerProveedoresAsync);
                var tareaActividades = _catalogoCacheService.ObtenerAsync("ActividadesSinCosecha", _actividadService.ObtenerActividadesSinCosechaAsync);
                var tareaPreparaciones = _catalogoCacheService.ObtenerAsync("Preparaciones", _actividadService.ObtenerPreparacionesAsync);
                var tareaEquipos = _catalogoCacheService.ObtenerAsync("Equipos", _actividadService.ObtenerEquiposAsync);
                var tareaUnidades = _catalogoCacheService.ObtenerAsync("UnidadesActividad", _actividadService.ObtenerUnidadesActividadAsync);

                // Predios: la clave de caché distingue Editar (catálogo completo) de Alta (filtrado por Zona/Municipio),
                // porque son dos listas potencialmente distintas.
                var tareaPredios = EsEdicion
                    ? _catalogoCacheService.ObtenerAsync("PrediosCatalogo", _actividadService.ObtenerPrediosCatalogoAsync)
                    : _catalogoCacheService.ObtenerAsync($"PrediosZonaMunicipio_{ZonaId}_{MunicipioId}",
                        ct => _actividadService.ObtenerPrediosAsync(ZonaId, MunicipioId, ct));

                await Task.WhenAll(
                    tareaVehiculos, tareaTractores, tareaJefes, tareaProveedores,
                    tareaActividades, tareaPreparaciones, tareaEquipos, tareaUnidades,
                    tareaPredios);

                Vehiculos = new ObservableCollection<VehiculoActividadModel>(tareaVehiculos.Result);
                TractoresCuadrillas = new ObservableCollection<TractorCuadrillaModel>(tareaTractores.Result);
                JefesCuadrilla = new ObservableCollection<JefeCuadrillaModel>(tareaJefes.Result);
                Proveedores = new ObservableCollection<ProveedorModel>(tareaProveedores.Result);
                Actividades = new ObservableCollection<ActividadAgricolaModel>(tareaActividades.Result);
                Preparaciones = new ObservableCollection<PreparacionModel>(tareaPreparaciones.Result);
                Equipos = new ObservableCollection<EquipoModel>(tareaEquipos.Result);
                UnidadesEquipo = new ObservableCollection<UnidadEquipoModel>(tareaUnidades.Result);
                Predios = new ObservableCollection<PredioModel>(tareaPredios.Result);

                if (EsEdicion)
                {
                    await CargarRegistroExistenteAsync();
                }
                else
                {
                    PredioSeleccionado = Predios.FirstOrDefault(p => p.IntGENPredioKey == PredioId);
                    MarcarInterno(); // <-- default: Interno seleccionado al dar de Alta
                }

                // Fire-and-forget: precalienta subactividades y detalle de materia prima en
                // segundo plano, sin bloquear al usuario ni el resto de InicializarAsync.
                // Solo se dispara si llegamos hasta aquí, es decir, hubo conexión y los
                // catálogos base ya se cargaron correctamente.
                _ = PrecalentarCatalogosAsync();
            }
            catch (Exception ex)
            {
                // Sin conexión Y sin copia local de los catálogos: no hay nada que mostrar en
                // el formulario (pasa la primera vez que se abre esta pantalla sin haber
                // tenido internet antes). Se distingue de un error real de programación con
                // un mensaje claro y se regresa, en vez de dejar el formulario a medio cargar.
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
        private async Task CargarRegistroExistenteAsync()
        {
            if (_registroActividadOriginal is null)
            {
                await Shell.Current.DisplayAlert("Error", "No fue posible cargar la información del registro. Regresa e intenta de nuevo.", "De acuerdo");
                return;
            }

            var reg = _registroActividadOriginal;

            // ── Encabezado: se resuelve contra los catálogos ya cargados arriba en InicializarAsync ──
            Id = reg.VchID;
            Fecha = reg.DtmFecha;
            Observaciones = reg.VchObservacionActividad;
            Inicial = reg.VchHorometroInicial ?? "0";
            Final = reg.VchHorometroFinal ?? "0";
            ProduccionInicial = TimeSpan.TryParse(reg.VchHrsProductivasInicial, out var pi) ? pi : TimeSpan.Zero;
            ProduccionFinal = TimeSpan.TryParse(reg.VchHrsProductivasFinal, out var pf) ? pf : TimeSpan.Zero;

            // Imagen existente (si la hay). VchNombreImagen ya trae la URL completa desde el backend
            // (mismo patrón que el Xamarin viejo en RegistroActividades.Editar()).
            if (!string.IsNullOrEmpty(reg.VchNombreImagen))
            {
                ImagenPath = reg.VchNombreImagen;
                imagenNombre = Path.GetFileName(new Uri(reg.VchNombreImagen).LocalPath);

                // Se conservan para poder distinguir en GuardarEdicionAsync si el usuario dejó la
                // imagen tal cual (no se debe volver a "subir", solo reenviar esta referencia) o
                // si la reemplazó por una nueva (foto/galería, ahí sí hay que subirla).
                _rutaArchivoImagenOriginal = reg.VchNombreImagen;
                _nombreImagenOriginal = imagenNombre;
            }

            PredioSeleccionado = Predios.FirstOrDefault(p => p.IntGENPredioKey == reg.IntGENPredioLink);
            VehiculoSeleccionado = Vehiculos.FirstOrDefault(v => v.IntGENUnidadParaActividadKey == reg.IntGENUnidadParaActividadLink);
            TractorCuadrillaSeleccionado = TractoresCuadrillas.FirstOrDefault(t => t.IntAGRTractoresCuadrillasKey == reg.IntAGRTractoresCuadrillasLink);
            JefeCuadrillaSeleccionado = JefesCuadrilla.FirstOrDefault(j => j.VchNombre == reg.VchJefeCuadrilla);
            ProveedorSeleccionado = Proveedores.FirstOrDefault(p => p.IntGENProveedorKey == reg.IntGENProveedorLink);

            if (reg.VchTipoCuadrilla != null && reg.VchTipoCuadrilla.StartsWith("Int", StringComparison.OrdinalIgnoreCase))
                MarcarInterno();
            else
                MarcarExterno();

            // ── Detalle: actividad realizada + insumos + implementos (los 3 GETs del paso 3) ──
            try
            {
                var actividadRealizada = await _actividadService.ObtenerActividadRealizadaAsync(reg.IntAGRRegistroActividadKey);
                if (actividadRealizada != null)
                {
                    // PASO 8: se guarda la key real de AGRActividadRealizada para poder mandar
                    // IntMovimiento = Actualizar con la key correcta en GuardarEdicionAsync.
                    // AJUSTAR el nombre de la propiedad si el modelo usa otro nombre de campo.
                    _actividadRealizadaKeyOriginal = actividadRealizada.IntAGRActividadRealizadaKey;

                    ActividadSeleccionada = Actividades.FirstOrDefault(a => a.IntAGRActividadKey == actividadRealizada.IntAGRActividadLink);

                    if (ActividadSeleccionada != null)
                    {
                        // Se espera aquí en vez de confiar en el disparo automático de OnActividadSeleccionadaChanged,
                        // para poder fijar la subactividad correcta justo después sin condición de carrera.
                        await CargarSubactividadesAsync(ActividadSeleccionada.IntAGREtapaKey, ActividadSeleccionada.IntAGRActividadKey);
                        SubactividadSeleccionada = Subactividades.FirstOrDefault(s => s.IntAGRSubActividadKey == actividadRealizada.IntAGRSubActividadLink);
                    }

                    CantidadActividad = actividadRealizada.DecValor.ToString();
                    NoPlantas = actividadRealizada.DecNoPlantas.ToString();
                    NoPersonas = actividadRealizada.DecNoPersonas.ToString();
                }

                var insumos = await _actividadService.ObtenerInsumosUtilizadosAsync(reg.IntAGRRegistroActividadKey);
                InsumosAgregados = new ObservableCollection<InsumoAgregadoItem>(insumos.Select(i => new InsumoAgregadoItem
                {
                    IntAGRInsumoUtilizadoKey = i.IntAGRInsumoUtilizadoKey,
                    IntAGRMastInsumoLink = i.IntAGRMastInsumoLink,
                    IntAGRActividadLink = i.IntAGRActividadLink,
                    Descripcion = i.VchInsumo,
                    Cantidad = i.DecValor,
                    Unidad = i.VchUnidad,
                    NoValeSalida = i.VchNoValeDeSalida,
                    Movimiento = MovimientoItem.Nada
                }));

                if (InsumosAgregados.Count > 0)
                {
                    NoValeSalida = InsumosAgregados[0].NoValeSalida;
                    NoValeSalidaHabilitado = false;
                }

                // PASADA 4: recalcular/explotar la materia prima de cada insumo ya guardado,
                // usando la Cantidad tal cual quedó registrada en servidor (no se pide de nuevo
                // al usuario). Esto habilita "Ver materia prima ›" también en modo Edición,
                // igual que ya funciona al Agregar() un insumo nuevo en modo Alta.
                // Se busca la PreparacionModel correspondiente por IntAGRMastInsumoKey para
                // obtener su DecDosis (necesaria para la ración); si un insumo no tiene match
                // en el catálogo de Preparaciones (p. ej. insumo dado de baja del catálogo),
                // simplemente se deja sin materia prima calculada (EsReceta = false).
                foreach (var insumoItem in InsumosAgregados)
                {
                    var preparacionRelacionada = Preparaciones
                        .FirstOrDefault(p => p.IntAGRMastInsumoKey == insumoItem.IntAGRMastInsumoLink);

                    if (preparacionRelacionada != null)
                    {
                        await RecalcularMateriaPrimaSilenciosoAsync(
                            insumoItem,
                            insumoItem.IntAGRActividadLink,
                            insumoItem.Cantidad,
                            preparacionRelacionada.DecDosis);
                    }
                }

                var implementos = await _actividadService.ObtenerImplementosUtilizadosAsync(reg.IntAGRRegistroActividadKey);
                ImplementosAgregados = new ObservableCollection<ImplementoAgregadoItem>(implementos.Select(i => new ImplementoAgregadoItem
                {
                    IntAGRImplementoUtilizadoKey = i.IntAGRImplementoUtilizadoKey,
                    IntGENImplementoLink = i.IntGENImplementoLink,
                    Descripcion = i.VchNombre,
                    Cantidad = i.DecCantidad,
                    Movimiento = MovimientoItem.Nada
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CargarRegistroExistenteAsync ERROR: {ex}");
                await Shell.Current.DisplayAlert("Advertencia (debug)", ex.ToString(), "De acuerdo");
            }
        }
        // ===================== PRECALENTADO DE CACHÉ OFFLINE =====================

        private const string ClavePreferenciaUltimoPrecalentado = "UltimoPrecalentadoCatalogosAgaveros";
        private const int DiasEntrePrecalentados = 1;

        /// <summary>
        /// Precarga en caché (SQLite, vía ICatalogoCacheService) las subactividades de todas las
        /// actividades y el detalle de materia prima de todas las preparaciones, para que el
        /// formulario funcione completo en modo offline sin que el usuario tenga que "visitar"
        /// cada combinación manualmente con conexión primero.
        ///
        /// Se ejecuta en segundo plano ("fire and forget") después de que InicializarAsync ya
        /// terminó y el formulario es usable, para no bloquear al usuario. Usa un límite de
        /// concurrencia (SemaphoreSlim) porque Preparaciones puede tener cientos de elementos
        /// (~380 en producción) y lanzar todas las llamadas a la vez saturaría el servidor.
        ///
        /// Solo corre una vez por día (ver ClavePreferenciaUltimoPrecalentado) para no repetir
        /// cientos de llamadas HTTP innecesarias cada vez que el usuario abre el formulario.
        /// </summary>
        private async Task PrecalentarCatalogosAsync()
        {
            try
            {
                var ultimaFechaTexto = Preferences.Default.Get(ClavePreferenciaUltimoPrecalentado, string.Empty);
                if (DateTime.TryParse(ultimaFechaTexto, out var ultimaFecha) &&
                    (DateTime.Now - ultimaFecha).TotalDays < DiasEntrePrecalentados)
                {
                    return; // ya se precalentó recientemente, no repetir
                }

                using var semaforo = new SemaphoreSlim(5);

                var tareasSubactividades = Actividades.Select(async actividad =>
                {
                    await semaforo.WaitAsync();
                    try
                    {
                        await _catalogoCacheService.ObtenerAsync(
                            $"Subactividades_{actividad.IntAGREtapaKey}_{actividad.IntAGRActividadKey}",
                            ct => _actividadService.ObtenerSubactividadesAsync(actividad.IntAGREtapaKey, actividad.IntAGRActividadKey, ct));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PrecalentarCatalogosAsync (subactividad {actividad.IntAGRActividadKey}) error: {ex.Message}");
                    }
                    finally
                    {
                        semaforo.Release();
                    }
                });

                var tareasDetalleInsumo = Preparaciones.Select(async preparacion =>
                {
                    await semaforo.WaitAsync();
                    try
                    {
                        await _catalogoCacheService.ObtenerAsync(
                            $"DetalleInsumo_{preparacion.IntAGRMastInsumoKey}",
                            ct => _actividadService.ObtenerDetalleInsumoAsync(preparacion.IntAGRMastInsumoKey, ct));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PrecalentarCatalogosAsync (insumo {preparacion.IntAGRMastInsumoKey}) error: {ex.Message}");
                    }
                    finally
                    {
                        semaforo.Release();
                    }
                });

                await Task.WhenAll(tareasSubactividades.Concat(tareasDetalleInsumo));

                Preferences.Default.Set(ClavePreferenciaUltimoPrecalentado, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                // El precalentado es una optimización silenciosa: si falla (p. ej. se corta la
                // conexión a la mitad), no se le notifica al usuario ni se rompe el formulario.
                System.Diagnostics.Debug.WriteLine($"PrecalentarCatalogosAsync error general: {ex}");
            }
        }
    }
}