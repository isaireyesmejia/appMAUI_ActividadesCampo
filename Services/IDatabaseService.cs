using SQLite;

namespace agaverosActividades.Services;

/// <summary>
/// Punto único de acceso a la base de datos SQLite local. Provee la conexión
/// asíncrona compartida y expone InicializarAsync para crear/migrar tablas.
/// Los servicios de outbox (PendingOperationService) y caché de catálogos
/// consumen esta conexión en vez de abrir la suya propia.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Conexión asíncrona compartida. Ya está lista para usarse (InicializarAsync
    /// se llama una sola vez desde App.xaml.cs al arrancar la app).
    /// </summary>
    SQLiteAsyncConnection Conexion { get; }

    /// <summary>
    /// Crea el archivo de base de datos si no existe y crea/actualiza las tablas
    /// registradas. Debe llamarse una sola vez al inicio de la app (App.xaml.cs),
    /// antes de que cualquier página intente usar la BD.
    /// </summary>
    Task InicializarAsync();

    /// <summary>
    /// Task que representa el estado de inicialización. App.xaml.cs dispara
    /// InicializarAsync() al arrancar sin esperarlo (no se puede await en un
    /// constructor); cualquier página o servicio que necesite tocar la BD debe
    /// hacer "await _databaseService.ListoAsync" ANTES de su primera consulta.
    /// En el caso normal (crear tablas toma milisegundos) ese await no se nota;
    /// solo protege el caso raro donde el usuario navega muy rápido al arrancar.
    /// </summary>
    Task ListoAsync { get; }
}