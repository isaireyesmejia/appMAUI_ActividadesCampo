using agaverosActividades.Models;
using agaverosActividades.Utilidades;

namespace agaverosActividades.Services;

public interface IUsuarioCacheService
{
    Task GuardarCredencialesAsync(string login, string nombre, string perfil, string password);
    Task<UsuarioCache?> ValidarCredencialesAsync(string login, string password);
}

public class UsuarioCacheService : IUsuarioCacheService
{
    private readonly IDatabaseService _databaseService;

    public UsuarioCacheService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task GuardarCredencialesAsync(string login, string nombre, string perfil, string password)
    {
        await _databaseService.ListoAsync;

        var hash = PasswordHasher.HashearPassword(password);

        var usuario = new UsuarioCache
        {
            Login = login,
            NombreUsuario = nombre,
            Perfil = perfil,
            PasswordHash = hash,
            FechaActualizacion = DateTime.Now
        };

        // InsertOrReplace: si ya existía (login previo), actualiza el hash
        // con la contraseña más reciente en vez de duplicar filas.
        await _databaseService.Conexion.InsertOrReplaceAsync(usuario);
    }

    public async Task<UsuarioCache?> ValidarCredencialesAsync(string login, string password)
    {
        await _databaseService.ListoAsync;

        var usuario = await _databaseService.Conexion
            .Table<UsuarioCache>()
            .Where(u => u.Login == login)
            .FirstOrDefaultAsync();

        if (usuario is null) return null;

        return PasswordHasher.VerificarPassword(password, usuario.PasswordHash)
            ? usuario
            : null;
    }
}