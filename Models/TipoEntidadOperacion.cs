namespace agaverosActividades.Models;

public enum TipoEntidadOperacion
{
    RegistroActividad = 0,
    ActividadRealizada = 1,
    InsumoUtilizado = 2,
    ImplementoUtilizado = 3,
    MateriaPrimaUtilizada = 4
}

public enum EstadoOperacion
{
    Pendiente = 0,
    Enviando = 1,
    Enviado = 2,
    Error = 3
}