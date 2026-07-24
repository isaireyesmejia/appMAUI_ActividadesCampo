namespace agaverosActividades.Services;

public interface ISesionApp
{
    string Login { get; }
    string NombreUsuario { get; }
    string Perfil { get; }
    string Modo { get; }
    string ApiBaseUrl { get; }

    void GuardarSesion(string login, string nombre, string perfil);
    void LimpiarSesion();
}