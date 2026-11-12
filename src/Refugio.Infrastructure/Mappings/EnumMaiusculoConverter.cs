using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Refugio.Infrastructure.Mappings;

/// <summary>
/// Grava enums como texto em maiúsculas com sublinhado (CentroComunitario → CENTRO_COMUNITARIO),
/// o mesmo formato que a API devolve no JSON.
/// </summary>
public sealed class EnumMaiusculoConverter<TEnum>() : ValueConverter<TEnum, string>(
    valor => ParaTexto(valor),
    texto => ParaEnum(texto))
    where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> Nomes =
        Enum.GetValues<TEnum>().ToDictionary(v => v, v => JsonNamingPolicy.SnakeCaseUpper.ConvertName(v.ToString()));

    private static readonly Dictionary<string, TEnum> Valores =
        Nomes.ToDictionary(par => par.Value, par => par.Key);

    private static string ParaTexto(TEnum valor) => Nomes[valor];

    private static TEnum ParaEnum(string texto) => Valores[texto];
}
