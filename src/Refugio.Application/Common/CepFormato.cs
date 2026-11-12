namespace Refugio.Application.Common;

public static class CepFormato
{
    /// <summary>Aceita "03601000" ou "03601-000" e devolve sempre "03601-000".</summary>
    public static bool TentarNormalizar(string? entrada, out string cep)
    {
        cep = string.Empty;
        var digitos = SomenteDigitos(entrada);
        if (digitos.Length != 8)
            return false;

        cep = $"{digitos[..5]}-{digitos[5..]}";
        return true;
    }

    public static bool EhValido(string? entrada) => TentarNormalizar(entrada, out _);

    public static string SomenteDigitos(string? entrada)
    {
        var texto = (entrada ?? string.Empty).Trim();
        // Só aceita dígitos e um hífen na posição usual; "0360-1000" ou letras não são CEP.
        if (texto.Length == 9 && texto[5] == '-')
            texto = texto.Remove(5, 1);
        return texto.All(char.IsAsciiDigit) ? texto : string.Empty;
    }
}
