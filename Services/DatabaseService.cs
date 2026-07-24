using agaverosActividades.Models;
using SQLite;

namespace agaverosActividades.Services;

public class DatabaseService : IDatabaseService
{
    private const string NombreArchivoBD = "agaverosActividades.db3";
    private bool _inicializado;
    private Task? _listoTask;

    public SQLiteAsyncConnection Conexion { get; }

    // ListoAsync: si App.xaml.cs aún no ha llamado a InicializarAsync() (caso
    // extraño, p.ej. un servicio se resuelve antes de tiempo), devolvemos un
    // Task ya completado para no bloquear indefinidamente — InicializarAsync()
    // igual crea las tablas la primera vez que se invoque.
    public Task ListoAsync => _listoTask ?? Task.CompletedTask;

    public DatabaseService()
    {
        var ruta = Path.Combine(FileSystem.AppDataDirectory, NombreArchivoBD);
        Conexion = new SQLiteAsyncConnection(ruta);
    }

    public Task InicializarAsync()
    {
        // Guard: si ya se disparó (o está en curso), devolvemos la misma Task
        // en vez de arrancar otra corrida en paralelo.
        _listoTask ??= InicializarInternoAsync();
        return _listoTask;
    }

    private async Task InicializarInternoAsync()
    {
        if (_inicializado) return;

        await Conexion.CreateTableAsync<PendingOperation>();
        await Conexion.CreateTableAsync<UsuarioCache>();
        await Conexion.CreateTableAsync<CatalogoCache>();

        _inicializado = true;
    }
}