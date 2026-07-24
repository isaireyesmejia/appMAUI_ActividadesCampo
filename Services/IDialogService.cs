using Microsoft.Maui.Controls;

namespace agaverosActividades.Services
{
    /// <summary>
    /// Abstracción de diálogos (alertas/confirmaciones) para que los ViewModels no dependan
    /// directamente de <see cref="Shell"/>/<see cref="Page"/>. Se inyecta por constructor,
    /// igual que <c>IActividadService</c> o <c>IAuthService</c>.
    ///
    /// Ventaja sobre exponer eventos (como hace hoy <c>LoginViewModel</c> con
    /// <c>MostrandoAlerta</c>): no hay que declarar un evento distinto por cada mensaje ni
    /// suscribir/desuscribir en el code-behind de cada Page — el ViewModel simplemente llama
    /// al servicio, y es mockeable en pruebas unitarias.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Alerta simple de un solo botón (equivalente a DisplayAlert con un solo "Cancel").</summary>
        Task DisplayAlertAsync(string titulo, string mensaje, string boton = "De acuerdo");

        /// <summary>Alerta con dos botones; regresa true si el usuario tocó el botón de aceptar.</summary>
        Task<bool> DisplayConfirmAsync(string titulo, string mensaje, string aceptar = "Confirmar", string cancelar = "Cancelar");
    }

    /// <summary>
    /// Implementación concreta sobre <see cref="Shell.Current"/>. Si más adelante alguna
    /// pantalla no vive dentro de Shell, esta es la única clase que habría que ajustar —
    /// los ViewModels no se tocan.
    /// </summary>
    public class DialogService : IDialogService
    {
        public Task DisplayAlertAsync(string titulo, string mensaje, string boton = "De acuerdo")
            => Shell.Current.DisplayAlert(titulo, mensaje, boton);

        public Task<bool> DisplayConfirmAsync(string titulo, string mensaje, string aceptar = "Confirmar", string cancelar = "Cancelar")
            => Shell.Current.DisplayAlert(titulo, mensaje, aceptar, cancelar);
    }
}