namespace Refugio.Application.Common;

/// <summary>
/// O contrato usa datas no fuso de São Paulo: "hoje", o filtro por data e o criadoEm.
/// </summary>
public static class FusoHorario
{
    public static readonly TimeZoneInfo SaoPaulo = Carregar();

    public static DateOnly HojeEmSaoPaulo(this TimeProvider relogio) =>
        DateOnly.FromDateTime(ParaSaoPaulo(relogio.GetUtcNow()).DateTime);

    public static DateTimeOffset ParaSaoPaulo(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, SaoPaulo);

    /// <summary>Instante atual em UTC, sem frações de segundo (o contrato mostra "10:02:11-03:00").</summary>
    public static DateTimeOffset AgoraUtcEmSegundos(this TimeProvider relogio)
    {
        var agora = relogio.GetUtcNow();
        return new DateTimeOffset(agora.Ticks - agora.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
    }

    private static TimeZoneInfo Carregar()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            // Imagens de contêiner sem tzdata: o Brasil não tem horário de verão desde 2019, então -03:00 fixo é equivalente.
            return TimeZoneInfo.CreateCustomTimeZone("America/Sao_Paulo", TimeSpan.FromHours(-3), "São Paulo", "BRT");
        }
    }
}
