using SQLite;

namespace agaverosActividades.Models;

[Table("UsuarioCache")]
public class UsuarioCache
{
    [PrimaryKey]
    public string Login { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; }
}