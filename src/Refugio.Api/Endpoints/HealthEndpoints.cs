using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Refugio.Infrastructure;

namespace Refugio.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// /health: API, banco e ViaCEP, com o detalhe de cada um (200 se Healthy/Degraded, 503 se Unhealthy).
    /// /health/live: só confirma que o processo responde.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = EscreverDetalhes })
            .DisableRateLimiting()
            .WithTags("Saúde");

        app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthChecksExtensions.TagVivo),
                ResponseWriter = EscreverDetalhes,
            })
            .DisableRateLimiting()
            .WithTags("Saúde");

        return app;
    }

    private static Task EscreverDetalhes(HttpContext http, HealthReport relatorio)
    {
        http.Response.ContentType = "application/json; charset=utf-8";
        return http.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = relatorio.Status.ToString(),
            duracaoMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds),
            verificacoes = relatorio.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                descricao = e.Value.Description,
                duracaoMs = Math.Round(e.Value.Duration.TotalMilliseconds),
            }),
        }, Json));
    }
}
