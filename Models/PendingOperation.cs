using SQLite;

namespace agaverosActividades.Models;

[Table("PendingOperation")]
public class PendingOperation
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public EstadoOperacion Estado { get; set; }

    public TipoEntidadOperacion TipoEntidad { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public int? IdTemporalLocal { get; set; }
    public int? IdTemporalPadre { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string? MensajeError { get; set; }
    public int Intentos { get; set; }
}