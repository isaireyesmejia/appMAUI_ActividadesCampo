namespace agaverosActividades.Models.Actividades;

public class ActividadRealizadaReturnModel
{
    public int RetVal { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// PASADA 6: nullable porque el backend regresa null en este campo cuando
    /// ActividadRealizadaAsync se llama como actualización (IntMovimiento = 2, modo Edición),
    /// solo lo regresa con valor en un insert (modo Alta). Con int (no nullable), System.Text.Json
    /// tronaba con JsonException al deserializar la respuesta de una actualización, aunque el
    /// guardado en sí ya se había completado correctamente — por eso salía "Ocurrió un problema
    /// al guardar" a pesar de que el registro sí quedaba guardado.
    /// Nota: al día de hoy el ViewModel no usa este valor de retorno en ningún lado
    /// (ActividadRealizadaAsync se llama sin capturar el resultado), así que este cambio no
    /// afecta ningún flujo existente.
    /// </summary>
    public int? ActividadRealizadaKey { get; set; }
}