namespace agaverosActividades.Services;

public class SessionService : ISessionService
{
    private const string ClaveModo = "sesion_modo";
    private const string ClaveUsuario = "sesion_usuario";
    private const string ClaveNombre = "sesion_nombre";
    private const string ClaveEsAdmin = "sesion_es_admin";

    public ModoOperacion Modo
    {
        get => Enum.Parse<ModoOperacion>(Preferences.Get(ClaveModo, nameof(ModoOperacion.Online)));
        set => Preferences.Set(ClaveModo, value.ToString());
    }

    public string? Usuario
    {
        get => Preferences.Get(ClaveUsuario, null);
        set => Preferences.Set(ClaveUsuario, value ?? string.Empty);
    }

    public string? NombreCompleto
    {
        get => Preferences.Get(ClaveNombre, null);
        set => Preferences.Set(ClaveNombre, value ?? string.Empty);
    }

    public bool EsAdministrador
    {
        get => Preferences.Get(ClaveEsAdmin, false);
        set => Preferences.Set(ClaveEsAdmin, value);
    }

    public bool EstaLogueado => !string.IsNullOrEmpty(Usuario);

    public void CerrarSesion()
    {
        Preferences.Remove(ClaveUsuario);
        Preferences.Remove(ClaveNombre);
        Preferences.Remove(ClaveEsAdmin);
        // Nota: NO borramos ClaveModo — así, si cierra sesión en campo sin señal,
        // no pierde su elección de Offline al volver a entrar.
    }
}