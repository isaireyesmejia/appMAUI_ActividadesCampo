using System.Security.Cryptography;

namespace agaverosActividades.Utilidades;

public static class PasswordHasher
{
    private const int TamanioSal = 16;
    private const int TamanioHash = 32;
    private const int Iteraciones = 100_000;

    public static string HashearPassword(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanioSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);
        return $"{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerificarPassword(string password, string hashAlmacenado)
    {
        var partes = hashAlmacenado.Split('.');
        if (partes.Length != 2) return false;

        var sal = Convert.FromBase64String(partes[0]);
        var hashEsperado = Convert.FromBase64String(partes[1]);
        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}