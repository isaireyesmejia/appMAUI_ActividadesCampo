namespace agaverosActividades.Models;

public class Usuario
{
    public string Id { get; set; }
    public string Nombre { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
}
