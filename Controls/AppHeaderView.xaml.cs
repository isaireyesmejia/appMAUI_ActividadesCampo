using System.Windows.Input;

namespace agaverosActividades.Controls;

/// <summary>
/// Encabezado unificado para todas las páginas de la app: identidad (logo + usuario/perfil),
/// fecha (siempre visible, en una píldora a la derecha), línea divisora de marca, y una franja
/// de título + acciones que cada página personaliza vía la propiedad Acciones.
///
/// USO TÍPICO en una página:
///
///   <controls:AppHeaderView Titulo="{Binding TituloPagina}" MostrarBotonAtras="True"
///                            ComandoAtras="{Binding CerrarCommand}">
///       <controls:AppHeaderView.Acciones>
///           <Grid ColumnDefinitions="*,*" ColumnSpacing="8">
///               <Button Text="Guardar" Command="{Binding GuardarCommand}" .../>
///               <Button Text="Cerrar" Command="{Binding CerrarCommand}" .../>
///           </Grid>
///       </controls:AppHeaderView.Acciones>
///   </controls:AppHeaderView>
///
/// USO en pantallas raíz sin título/regreso, con ícono de identidad (ej. cerrar sesión):
///
///   <controls:AppHeaderView MostrarFranjaTitulo="False"
///                            MostrarIconoIdentidad="True" IconoIdentidad="&#128274;"
///                            ComandoIdentidad="{Binding CerrarSesionCommand}" />
///
/// NOTA: Usuario y Perfil NO son bindable properties aquí a propósito — se resuelven por
/// herencia normal de BindingContext (todas las páginas de la app ya exponen Usuario/Perfil
/// en su ViewModel, así que no hace falta repetirlos al usar el control).
/// </summary>
public partial class AppHeaderView : ContentView
{
    public AppHeaderView()
    {
        InitializeComponent();
    }

    // ===================== TÍTULO =====================

    public static readonly BindableProperty TituloProperty =
        BindableProperty.Create(nameof(Titulo), typeof(string), typeof(AppHeaderView), string.Empty);

    public string Titulo
    {
        get => (string)GetValue(TituloProperty);
        set => SetValue(TituloProperty, value);
    }

    /// <summary>
    /// Controla si se muestra la franja completa de título + botón atrás. Default true, para no
    /// afectar a las pantallas ya migradas. Ponerlo en false en pantallas raíz (como Bienvenido)
    /// que no tienen título ni navegación hacia atrás y usan otro contenido (p. ej. tabs) en su lugar.
    /// </summary>
    public static readonly BindableProperty MostrarFranjaTituloProperty =
        BindableProperty.Create(nameof(MostrarFranjaTitulo), typeof(bool), typeof(AppHeaderView), true);

    public bool MostrarFranjaTitulo
    {
        get => (bool)GetValue(MostrarFranjaTituloProperty);
        set => SetValue(MostrarFranjaTituloProperty, value);
    }

    // ===================== BOTÓN ATRÁS =====================

    /// <summary>Si es false (pantallas raíz, sin navegación hacia atrás), se oculta la flecha.</summary>
    public static readonly BindableProperty MostrarBotonAtrasProperty =
        BindableProperty.Create(nameof(MostrarBotonAtras), typeof(bool), typeof(AppHeaderView), true);

    public bool MostrarBotonAtras
    {
        get => (bool)GetValue(MostrarBotonAtrasProperty);
        set => SetValue(MostrarBotonAtrasProperty, value);
    }

    /// <summary>Comando que se ejecuta al tocar la flecha de regreso (normalmente CerrarCommand
    /// o un comando de navegación específico de la página).</summary>
    public static readonly BindableProperty ComandoAtrasProperty =
        BindableProperty.Create(nameof(ComandoAtras), typeof(ICommand), typeof(AppHeaderView));

    public ICommand ComandoAtras
    {
        get => (ICommand)GetValue(ComandoAtrasProperty);
        set => SetValue(ComandoAtrasProperty, value);
    }

    /// <summary>Ícono de la flecha de regreso. Default "‹". Permite cambiarlo si alguna
    /// pantalla necesita otro glifo (p. ej. una "X" de cerrar en vez de flecha).</summary>
    public static readonly BindableProperty IconoAtrasProperty =
        BindableProperty.Create(nameof(IconoAtras), typeof(string), typeof(AppHeaderView), "\u2039");

    public string IconoAtras
    {
        get => (string)GetValue(IconoAtrasProperty);
        set => SetValue(IconoAtrasProperty, value);
    }

    // ===================== ACCIONES (slot de contenido personalizado) =====================

    /// <summary>
    /// Contenido que cada página inyecta en la franja de acciones (botones Guardar/Cerrar,
    /// chips de Agregar/Editar/Limpiar/Eliminar, etc.). Se asigna con la sintaxis de property
    /// element &lt;controls:AppHeaderView.Acciones&gt;...&lt;/controls:AppHeaderView.Acciones&gt;
    /// porque "Content" ya está tomado por el layout interno del propio control.
    /// </summary>
    public static readonly BindableProperty AccionesProperty =
        BindableProperty.Create(nameof(Acciones), typeof(View), typeof(AppHeaderView),
            propertyChanged: OnAccionesChanged);

    public View Acciones
    {
        get => (View)GetValue(AccionesProperty);
        set => SetValue(AccionesProperty, value);
    }

    private static void OnAccionesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeaderView header && header.AccionesHost != null)
            header.AccionesHost.Content = newValue as View;
    }

    // ===================== ÍCONO DE IDENTIDAD (acción junto a la fecha) =====================

    /// <summary>
    /// Muestra un ícono chico junto a la fecha, para acciones que no son navegación hacia atrás
    /// (p. ej. cerrar sesión en la pantalla raíz). Default false: no aparece en pantallas
    /// migradas existentes a menos que se active explícitamente.
    /// </summary>
    public static readonly BindableProperty MostrarIconoIdentidadProperty =
        BindableProperty.Create(nameof(MostrarIconoIdentidad), typeof(bool), typeof(AppHeaderView), false);

    public bool MostrarIconoIdentidad
    {
        get => (bool)GetValue(MostrarIconoIdentidadProperty);
        set => SetValue(MostrarIconoIdentidadProperty, value);
    }

    public static readonly BindableProperty IconoIdentidadProperty =
        BindableProperty.Create(nameof(IconoIdentidad), typeof(string), typeof(AppHeaderView), string.Empty);

    public string IconoIdentidad
    {
        get => (string)GetValue(IconoIdentidadProperty);
        set => SetValue(IconoIdentidadProperty, value);
    }

    public static readonly BindableProperty ComandoIdentidadProperty =
        BindableProperty.Create(nameof(ComandoIdentidad), typeof(ICommand), typeof(AppHeaderView));

    public ICommand ComandoIdentidad
    {
        get => (ICommand)GetValue(ComandoIdentidadProperty);
        set => SetValue(ComandoIdentidadProperty, value);
    }

    // ===================== FECHA =====================

    /// <summary>
    /// Fecha de hoy, formateada en español. Al ser una propiedad de solo lectura evaluada una
    /// vez por instancia (no cambia mientras la página está abierta), no requiere notificar
    /// cambios: basta con exponerla como propiedad CLR normal para que el binding la lea al
    /// crear el control.
    /// </summary>
    public string FechaHoy => DateTime.Now.ToString("dd MMM yyyy", new System.Globalization.CultureInfo("es-MX"));
}