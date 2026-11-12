using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Refugio.Api.Extensions;

public static class PoliticasDeLimite
{
    /// <summary>Login e cadastro: por IP, para frear tentativa de senha por força bruta.</summary>
    public const string Autenticacao = "autenticacao";

    /// <summary>Criação de reserva: por usuário.</summary>
    public const string Reservas = "reservas";
}

/// <summary>Seção "RateLimit" do appsettings. Cada limite é "N requisições por janela de S segundos".</summary>
public sealed class LimitesDeRequisicao
{
    public Limite Global { get; set; } = new() { Requisicoes = 100, JanelaSegundos = 60 };
    public Limite Autenticacao { get; set; } = new() { Requisicoes = 10, JanelaSegundos = 60 };
    public Limite Reservas { get; set; } = new() { Requisicoes = 10, JanelaSegundos = 60 };

    public sealed class Limite
    {
        public int Requisicoes { get; set; }
        public int JanelaSegundos { get; set; }

        public FixedWindowRateLimiterOptions ParaJanelaFixa() => new()
        {
            PermitLimit = Requisicoes,
            Window = TimeSpan.FromSeconds(JanelaSegundos),
            QueueLimit = 0,
        };
    }
}

public static class RateLimitExtensions
{
    public static IServiceCollection AddLimitesDeRequisicao(this IServiceCollection services, IConfiguration configuration)
    {
        var limites = configuration.GetSection("RateLimit").Get<LimitesDeRequisicao>() ?? new LimitesDeRequisicao();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                RateLimitPartition.GetFixedWindowLimiter(ChavePorUsuario(http), _ => limites.Global.ParaJanelaFixa()));

            options.AddPolicy(PoliticasDeLimite.Autenticacao, http =>
                RateLimitPartition.GetFixedWindowLimiter(ChavePorIp(http), _ => limites.Autenticacao.ParaJanelaFixa()));

            options.AddPolicy(PoliticasDeLimite.Reservas, http =>
                RateLimitPartition.GetFixedWindowLimiter(ChavePorUsuario(http), _ => limites.Reservas.ParaJanelaFixa()));

            options.OnRejected = EscreverLimiteExcedido;
        });

        return services;
    }

    /// <summary>Usuário logado conta pelo id do token; anônimo conta pelo IP.</summary>
    private static string ChavePorUsuario(HttpContext http) =>
        http.User.FindFirst("sub")?.Value is { } id ? $"usuario:{id}" : ChavePorIp(http);

    private static string ChavePorIp(HttpContext http) =>
        $"ip:{http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";

    /// <summary>429 em ProblemDetails, com Retry-After em segundos para o app mostrar a contagem.</summary>
    private static async ValueTask EscreverLimiteExcedido(OnRejectedContext contexto, CancellationToken ct)
    {
        var segundos = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera)
            ? Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds))
            : 60;

        var http = contexto.HttpContext;
        http.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);

        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas requisições",
                Detail = $"Você fez muitas tentativas seguidas. Tente de novo em {segundos} segundos.",
            },
        });
    }
}
