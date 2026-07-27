namespace agaverosActividades.Models;

/// <summary>
/// Envoltorio genérico usado por SearchablePickerPage para poder mostrar
/// y filtrar cualquier tipo de objeto sin acoplarse a un modelo específico.
/// </summary>
public class SearchablePickerItem
{
    public object Valor { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
}
