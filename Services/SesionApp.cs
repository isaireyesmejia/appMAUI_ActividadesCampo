using agaverosActividades.Constants;
using Microsoft.Maui.Storage;

namespace agaverosActividades.Services;

public class SesionApp : ISesionApp
{
    public string Login => Preferences.Get(PreferenceKeys.LOGIN, string.Empty);
    public string NombreUsuario => Preferences.Get(PreferenceKeys.NOMBRE, string.Empty);
    public string Perfil => Preferences.Get(PreferenceKeys.PERFIL, string.Empty);
    public string Modo => Preferences.Get(PreferenceKeys.MODO, string.Empty);
    public string ApiBaseUrl => Preferences.Get(PreferenceKeys.API, ApiEndpoints.BASE_API);

    public void GuardarSesion(string login, string nombre, string perfil)
    {
        Preferences.Set(PreferenceKeys.API, ApiEndpoints.BASE_API);
        Preferences.Set(PreferenceKeys.LOGIN, login);
        Preferences.Set(PreferenceKeys.NOMBRE, nombre);
        Preferences.Set(PreferenceKeys.PERFIL, perfil);
    }

    public void LimpiarSesion()
    {
        Preferences.Remove(PreferenceKeys.LOGIN);
        Preferences.Remove(PreferenceKeys.NOMBRE);
        Preferences.Remove(PreferenceKeys.PERFIL);
        Preferences.Remove(PreferenceKeys.MODO);
    }
}