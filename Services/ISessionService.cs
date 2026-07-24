namespace agaverosActividades.Services;

public enum ModoOperacion
{
    Online,
    Offline
}

public interface ISessionService
{
    ModoOperacion Modo { get; set; }
    string? Usuario { get; set; }
    string? NombreCompleto { get; set; }
    bool EsAdministrador { get; set; }
    bool EstaLogueado { get; }

    void CerrarSesion();
}