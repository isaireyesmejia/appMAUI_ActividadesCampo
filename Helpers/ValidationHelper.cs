namespace agaverosActividades.Helpers;

public static class ValidationHelper
{
    public static bool EsEmailValido(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool NoEstaVacio(string texto)
    {
        return !string.IsNullOrWhiteSpace(texto);
    }

    public static bool EsFechaValida(DateTime fecha)
    {
        return fecha != DateTime.MinValue && fecha <= DateTime.Now;
    }

    public static bool TieneLongitudMinima(string texto, int minimo)
    {
        return !string.IsNullOrWhiteSpace(texto) && texto.Length >= minimo;
    }

    public static string Normalizar(string texto)
    {
        return texto?.Trim() ?? string.Empty;
    }
}
